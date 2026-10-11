# Black Myth: Wukong Benchmark Automation

C# application for automating Black Myth: Wukong Benchmark Tool.

## Features

- Launches the benchmark through Steam
- Applies LOW and HIGH graphics presets
- Automates UI interactions using WinAPI
- Captures benchmark screenshots
- Extracts FPS values using Windows OCR
- Generates a Markdown report

## Requirements

- Windows 10/11
- .NET 10 SDK
- Steam and Black Myth: Wukong Benchmark Tool
- Russian Windows OCR language support

## Commands

Build: `dotnet build`

LOW test: `dotnet run -- --low`

HIGH test: `dotnet run -- --high`

Both tests: `dotnet run -- --all`

Read screenshot: `dotnet run -- --read`

Generate report: `dotnet run -- --report`

## Test hardware

- CPU: Intel Core i5-12500H
- GPU: NVIDIA GeForce RTX 3060 Laptop GPU (6 GB)
- RAM: 16 GB
- OS: Windows 10
- Resolution: 1920 x 1080

## Results

| Metric | LOW | HIGH |
|---|---:|---:|
| Average FPS | 84 | 53 |
| Maximum FPS | 121 | 74 |
| Minimum FPS | 34 | 39 |
| 5th percentile FPS | 38 | 44 |

See `results/report.md` for the report and screenshots.

## Limitations

The benchmark installation path and hardware specifications are currently configured for the test machine. UI automation relies on fixed click coordinates and timing. The full `--all` workflow is experimental and has not yet been successfully verified end-to-end. FPS extraction may require manual verification.
