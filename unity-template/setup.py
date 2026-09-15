#!/usr/bin/env python3
"""새 Unity 프로젝트에 공용 셋업을 적용한다.

사용법:
    python3 setup.py <유니티_프로젝트_경로> [--force]

<유니티_프로젝트_경로>는 Assets/Packages/ProjectSettings가 있는 폴더.
이 스크립트는 어디서 실행해도 되고(템플릿 파일은 스크립트 위치 기준으로 찾는다),
이미 적용된 항목은 건너뛴다(재실행 안전).

주의: 대상 프로젝트를 연 Unity 에디터가 떠 있으면 중단한다. 에디터가 ProjectSettings를
메모리에 들고 있다가 종료 시 덮어써서 Sorting Layers 등이 날아가기 때문.
--force로 무시할 수 있지만 권장하지 않는다.

적용 내용은 SETUP.md 참고.
"""
import io
import json
import re
import shutil
import subprocess
import sys
import urllib.request
import zipfile
from pathlib import Path

TEMPLATE_DIR = Path(__file__).resolve().parent

# R3 코어는 NuGet 배포라 UPM으로 안 온다. NuGetForUnity의 자동 복원은
# R3.Unity 컴파일 에러가 먼저 나면 도메인 리로드가 막혀 동작하지 않으므로
# (닭-달걀), 여기서 dll을 직접 받아 Assets/Packages에 NuGetForUnity 레이아웃으로 깐다.
# R3.Unity.asmdef의 precompiledReferences + R3.dll의 어셈블리 참조를 모두 만족시키는 목록.
# System.Buffers / System.Memory는 Unity가 netstandard 2.1 shim으로 제공하므로 제외.
NUGET_PACKAGES = [
    ("R3", "1.3.1"),
    ("Microsoft.Bcl.TimeProvider", "8.0.0"),
    ("Microsoft.Bcl.AsyncInterfaces", "8.0.0"),
    ("System.ComponentModel.Annotations", "5.0.0"),
    ("System.Runtime.CompilerServices.Unsafe", "6.0.0"),
    ("System.Threading.Channels", "8.0.0"),
]
NUGET_URL = "https://api.nuget.org/v3-flatcontainer/{id_lower}/{ver}/{id_lower}.{ver}.nupkg"
TFM_PREFERENCE = ("netstandard2.1", "netstandard2.0")

# 기본 폰트: Gothic A1 (Google Fonts, OFL). 한글 지원 + 굵기 9종이라 본문·제목 다 커버된다.
# OFL.txt는 라이선스상 배포 시 동봉해야 하므로 같이 받는다.
FONT_DIR_URL = "https://raw.githubusercontent.com/google/fonts/main/ofl/gothica1/"
FONT_FILES = ["GothicA1-Regular.ttf", "GothicA1-Bold.ttf", "GothicA1-Black.ttf", "OFL.txt"]

GIT_PACKAGES = {
    "com.cysharp.r3": "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.gameframex.unity.demigiant.dotween": "https://github.com/gameframex/com.gameframex.unity.demigiant.dotween.git",
    "com.github-glitchenzo.nugetforunity": "https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity",
}
# Unity Cloud 연결 없이는 Package Manager 에러를 띄우는 패키지들
REMOVE_PACKAGES = ["com.unity.purchasing", "com.unity.ads", "com.unity.analytics"]

SORTING_LAYERS = [
    ("Ground", 315482641),
    ("GroundEffect", 785310442),
    ("Shadow", 923846713),
    ("Default", 0),
    ("Actor", 641937584),
    ("Effect", 469823157),
    ("WorldUI", 85296341),
]


def log(msg):
    print(f"  {msg}")


def copy_if_missing(src, dst, label):
    if dst.exists():
        log(f"[skip] {label} 이미 있음")
    else:
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy(src, dst)
        log(f"[ok]   {label} 복사")


def restore_nuget(project):
    """NuGet dll들을 Assets/Packages에 NuGetForUnity 레이아웃으로 배치."""
    dest_root = project / "Assets" / "Packages"
    for pid, ver in NUGET_PACKAGES:
        target = dest_root / f"{pid}.{ver}"
        if target.exists():
            log(f"[skip] NuGet {pid} {ver} 이미 있음")
            continue
        url = NUGET_URL.format(id_lower=pid.lower(), ver=ver)
        try:
            with urllib.request.urlopen(url, timeout=60) as resp:
                blob = resp.read()
        except Exception as e:  # 오프라인 등
            log(f"[!!]   NuGet {pid} {ver} 다운로드 실패: {e}")
            continue
        with zipfile.ZipFile(io.BytesIO(blob)) as z:
            names = z.namelist()
            tfm = next((t for t in TFM_PREFERENCE if any(n.startswith(f"lib/{t}/") for n in names)), None)
            if tfm is None:
                log(f"[!!]   {pid} {ver}: netstandard 2.0/2.1 lib 없음 — 수동 확인 필요")
                continue
            out = target / "lib" / tfm
            out.mkdir(parents=True, exist_ok=True)
            for n in names:
                if n.startswith(f"lib/{tfm}/") and n.endswith(".dll"):
                    (out / Path(n).name).write_bytes(z.read(n))
        log(f"[ok]   NuGet {pid} {ver} ({tfm})")

    config = ['<?xml version="1.0" encoding="utf-8"?>', "<packages>"]
    for pid, ver in sorted(NUGET_PACKAGES):
        manual = ' manuallyInstalled="true"' if pid == "R3" else ""
        config.append(f'  <package id="{pid}" version="{ver}"{manual} />')
    config += ["</packages>", ""]
    (project / "Assets" / "packages.config").write_text("\n".join(config))


def unity_is_open(project):
    """대상 프로젝트를 연 Unity 에디터가 실행 중인지."""
    try:
        out = subprocess.run(["ps", "-axo", "command"], capture_output=True, text=True, timeout=10).stdout
    except Exception:
        return False
    for line in out.splitlines():
        if "Unity.app/Contents/MacOS/Unity" in line and "-batchMode" not in line:
            m = re.search(r"-projectpath\s+(\S+)", line, re.I)
            if m and Path(m.group(1)).resolve() == project:
                return True
    return False


def install_font(project):
    """Gothic A1을 Assets/Fonts에 받고, TMP 기본 폰트로 지정하는 에디터 스크립트를 깐다."""
    font_dir = project / "Assets" / "Fonts"
    for name in FONT_FILES:
        dest = font_dir / name
        if dest.exists():
            log(f"[skip] 폰트 {name} 이미 있음")
            continue
        try:
            with urllib.request.urlopen(FONT_DIR_URL + name, timeout=60) as resp:
                blob = resp.read()
        except Exception as e:
            log(f"[!!]   폰트 {name} 다운로드 실패: {e}")
            continue
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_bytes(blob)
        log(f"[ok]   폰트 {name}")

    # Assets/Editor에 둬야 Assembly-CSharp-Editor로 잡혀 TextMeshPro가 자동 참조된다.
    copy_if_missing(
        TEMPLATE_DIR / "DefaultFontSetup.cs",
        project / "Assets" / "Editor" / "DefaultFontSetup.cs",
        "Assets/Editor/DefaultFontSetup.cs",
    )


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("-")]
    force = "--force" in sys.argv
    if len(args) != 1:
        sys.exit(__doc__)
    project = Path(args[0]).resolve()
    manifest_path = project / "Packages" / "manifest.json"
    if not manifest_path.exists():
        sys.exit(f"Unity 프로젝트가 아님: {manifest_path} 없음")
    if unity_is_open(project) and not force:
        sys.exit(
            "이 프로젝트를 연 Unity 에디터가 실행 중이다. 먼저 종료할 것.\n"
            "(에디터가 ProjectSettings를 덮어써서 Sorting Layers 등이 날아간다. "
            "정말 강행하려면 --force)"
        )

    print(f"적용 대상: {project}")

    # 1. .gitignore / .gitattributes — git 리포 루트를 찾아서 배치
    git_root = next((p for p in [project, *project.parents] if (p / ".git").exists()), project)
    copy_if_missing(TEMPLATE_DIR / "gitignore", git_root / ".gitignore", f".gitignore ({git_root})")
    copy_if_missing(TEMPLATE_DIR / "gitattributes", git_root / ".gitattributes", f".gitattributes ({git_root})")

    # 2. manifest.json — git 패키지 추가, 서비스 연결 필요 패키지 제거
    manifest = json.loads(manifest_path.read_text())
    deps = manifest["dependencies"]
    changed = False
    for name, url in GIT_PACKAGES.items():
        if name not in deps:
            deps[name] = url
            log(f"[ok]   패키지 추가: {name}")
            changed = True
    for name in REMOVE_PACKAGES:
        if name in deps:
            del deps[name]
            log(f"[ok]   패키지 제거: {name}")
            changed = True
    if changed:
        manifest["dependencies"] = dict(sorted(deps.items()))
        manifest_path.write_text(json.dumps(manifest, indent=2) + "\n")
    else:
        log("[skip] manifest.json 변경 없음")

    # 3. NuGet 복원 (R3 코어 + 의존 dll) + packages.config 생성
    restore_nuget(project)

    # 4. asmdef
    copy_if_missing(TEMPLATE_DIR / "Game.asmdef", project / "Assets" / "Scripts" / "Game.asmdef", "Assets/Scripts/Game.asmdef")
    copy_if_missing(TEMPLATE_DIR / "Game.Editor.asmdef", project / "Assets" / "Scripts" / "Editor" / "Game.Editor.asmdef", "Assets/Scripts/Editor/Game.Editor.asmdef")

    # 5. 기본 폰트 (Gothic A1 + TMP 기본 지정 스크립트)
    install_font(project)

    # 6. Active Input Handling → Input System (New)
    ps_path = project / "ProjectSettings" / "ProjectSettings.asset"
    ps = ps_path.read_text()
    ps2 = re.sub(r"^(  activeInputHandler: )\d+$", r"\g<1>1", ps, flags=re.M)
    if ps2 != ps:
        ps_path.write_text(ps2)
        log("[ok]   activeInputHandler → 1 (Input System New)")
    else:
        log("[skip] activeInputHandler 이미 1")

    # 7. Transparency Sort Mode → Custom Axis (0,0,1)
    gs_path = project / "ProjectSettings" / "GraphicsSettings.asset"
    gs = gs_path.read_text()
    gs2 = re.sub(r"^(  m_TransparencySortMode: )\d+$", r"\g<1>2", gs, flags=re.M)
    gs2 = re.sub(r"^(  m_TransparencySortAxis: ){.*}$", r"\g<1>{x: 0, y: 0, z: 1}", gs2, flags=re.M)
    if gs2 != gs:
        gs_path.write_text(gs2)
        log("[ok]   Transparency Sort: Custom Axis (0,0,1)")
    else:
        log("[skip] Transparency Sort 이미 설정됨")

    # 8. Sorting Layers
    tm_path = project / "ProjectSettings" / "TagManager.asset"
    tm = tm_path.read_text()
    if "name: Actor" in tm:
        log("[skip] Sorting Layers 이미 설정됨")
    else:
        block = "  m_SortingLayers:\n" + "".join(
            f"  - name: {n}\n    uniqueID: {u}\n    locked: 0\n" for n, u in SORTING_LAYERS
        )
        tm2, count = re.subn(
            r"  m_SortingLayers:\n(?:  - name: .*\n    uniqueID: .*\n    locked: .*\n)*",
            block, tm, count=1,
        )
        if count:
            tm_path.write_text(tm2)
            log("[ok]   Sorting Layers 추가: " + " / ".join(n for n, _ in SORTING_LAYERS))
        else:
            log("[!!]   TagManager.asset에서 m_SortingLayers를 못 찾음 — 수동 확인 필요")

    print("완료. 에디터를 열면 UPM 패키지가 설치된다 (NuGet dll은 이미 배치됨).")
    print("DOTween은 Tools > Demigiant > DOTween Utility Panel에서 Setup 한 번.")


if __name__ == "__main__":
    main()
