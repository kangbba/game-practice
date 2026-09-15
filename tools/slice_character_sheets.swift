#!/usr/bin/env swift

import Foundation
import CoreGraphics
import ImageIO
import UniformTypeIdentifiers

struct Part {
    let name: String
    let row: Int
    let column: Int
}

let parts = [
    Part(name: "Head", row: 0, column: 0),
    Part(name: "Torso", row: 0, column: 1),
    Part(name: "Hair", row: 0, column: 2),
    Part(name: "UpperArm", row: 1, column: 0),
    Part(name: "Forearm", row: 1, column: 1),
    Part(name: "Weapon", row: 1, column: 2),
    Part(name: "RearLeg", row: 2, column: 0),
    Part(name: "FrontLeg", row: 2, column: 1),
    Part(name: "Cape", row: 2, column: 2),
]

func fail(_ message: String) -> Never {
    FileHandle.standardError.write(Data((message + "\n").utf8))
    exit(1)
}

guard CommandLine.arguments.count == 4 else {
    fail("usage: slice_character_sheets.swift <sheet.png> <character-name> <output-directory>")
}

let inputURL = URL(fileURLWithPath: CommandLine.arguments[1])
let characterName = CommandLine.arguments[2]
let outputDirectory = URL(fileURLWithPath: CommandLine.arguments[3], isDirectory: true)

guard let source = CGImageSourceCreateWithURL(inputURL as CFURL, nil),
      let image = CGImageSourceCreateImageAtIndex(source, 0, nil) else {
    fail("could not load \(inputURL.path)")
}

let width = image.width
let height = image.height
let bytesPerRow = width * 4
var rgba = [UInt8](repeating: 0, count: height * bytesPerRow)

guard let colorSpace = CGColorSpace(name: CGColorSpace.sRGB),
      let context = CGContext(
        data: &rgba,
        width: width,
        height: height,
        bitsPerComponent: 8,
        bytesPerRow: bytesPerRow,
        space: colorSpace,
        bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue
      ) else {
    fail("could not create RGBA scan buffer")
}

context.draw(image, in: CGRect(x: 0, y: 0, width: width, height: height))
try? FileManager.default.createDirectory(at: outputDirectory, withIntermediateDirectories: true)

for part in parts {
    let x0 = part.column * width / 3
    let x1 = (part.column + 1) * width / 3
    // CGImage cropping and the RGBA scan buffer both address rows from the image top.
    let y0 = part.row * height / 3
    let y1 = (part.row + 1) * height / 3

    let cellWidth = x1 - x0
    let cellHeight = y1 - y0
    var visited = [Bool](repeating: false, count: cellWidth * cellHeight)
    var largestComponent: [(Int, Int)] = []

    for localY in 0..<cellHeight {
        for localX in 0..<cellWidth {
            let localIndex = localY * cellWidth + localX
            if visited[localIndex] { continue }
            visited[localIndex] = true
            let globalX = x0 + localX
            let globalY = y0 + localY
            if rgba[globalY * bytesPerRow + globalX * 4 + 3] <= 20 { continue }

            var component: [(Int, Int)] = []
            var queue = [(localX, localY)]
            var queueIndex = 0
            while queueIndex < queue.count {
                let (cx, cy) = queue[queueIndex]
                queueIndex += 1
                component.append((x0 + cx, y0 + cy))

                for (nx, ny) in [(cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1)] {
                    if nx < 0 || nx >= cellWidth || ny < 0 || ny >= cellHeight { continue }
                    let nextIndex = ny * cellWidth + nx
                    if visited[nextIndex] { continue }
                    visited[nextIndex] = true
                    let alpha = rgba[(y0 + ny) * bytesPerRow + (x0 + nx) * 4 + 3]
                    if alpha > 20 { queue.append((nx, ny)) }
                }
            }

            if component.count > largestComponent.count {
                largestComponent = component
            }
        }
    }

    guard !largestComponent.isEmpty else {
        fail("no opaque pixels found for \(characterName)_\(part.name)")
    }

    // Keep only the largest connected island in the cell. This discards hair, weapons, or
    // ribbons that slightly spill in from a neighboring cell in AI-generated source sheets.
    var selected = [Bool](repeating: false, count: width * height)
    for (x, y) in largestComponent {
        for dy in -2...2 {
            for dx in -2...2 {
                let nx = x + dx
                let ny = y + dy
                if nx < x0 || nx >= x1 || ny < y0 || ny >= y1 { continue }
                if rgba[ny * bytesPerRow + nx * 4 + 3] > 0 {
                    selected[ny * width + nx] = true
                }
            }
        }
    }

    var minX = x1
    var minY = y1
    var maxX = x0 - 1
    var maxY = y0 - 1
    for y in y0..<y1 {
        for x in x0..<x1 where selected[y * width + x] {
            minX = min(minX, x)
            minY = min(minY, y)
            maxX = max(maxX, x)
            maxY = max(maxY, y)
        }
    }

    let padding = 2
    minX = max(x0, minX - padding)
    minY = max(y0, minY - padding)
    maxX = min(x1 - 1, maxX + padding)
    maxY = min(y1 - 1, maxY + padding)

    let cropRect = CGRect(
        x: minX,
        y: minY,
        width: maxX - minX + 1,
        height: maxY - minY + 1
    )

    let outputWidth = Int(cropRect.width)
    let outputHeight = Int(cropRect.height)
    var outputRGBA = [UInt8](repeating: 0, count: outputWidth * outputHeight * 4)
    for outputY in 0..<outputHeight {
        for outputX in 0..<outputWidth {
            let sourceX = minX + outputX
            let sourceY = minY + outputY
            if !selected[sourceY * width + sourceX] { continue }
            let sourceIndex = sourceY * bytesPerRow + sourceX * 4
            let outputIndex = (outputY * outputWidth + outputX) * 4
            outputRGBA[outputIndex] = rgba[sourceIndex]
            outputRGBA[outputIndex + 1] = rgba[sourceIndex + 1]
            outputRGBA[outputIndex + 2] = rgba[sourceIndex + 2]
            outputRGBA[outputIndex + 3] = rgba[sourceIndex + 3]
        }
    }

    guard let outputContext = CGContext(
        data: &outputRGBA,
        width: outputWidth,
        height: outputHeight,
        bitsPerComponent: 8,
        bytesPerRow: outputWidth * 4,
        space: colorSpace,
        bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue
    ), let cropped = outputContext.makeImage() else {
        fail("could not render \(characterName)_\(part.name)")
    }

    let outputURL = outputDirectory.appendingPathComponent("\(characterName)_\(part.name).png")
    guard let destination = CGImageDestinationCreateWithURL(
        outputURL as CFURL,
        UTType.png.identifier as CFString,
        1,
        nil
    ) else {
        fail("could not create \(outputURL.path)")
    }

    CGImageDestinationAddImage(destination, cropped, nil)
    guard CGImageDestinationFinalize(destination) else {
        fail("could not write \(outputURL.path)")
    }

    print("\(outputURL.lastPathComponent): \(cropped.width)x\(cropped.height)")
}
