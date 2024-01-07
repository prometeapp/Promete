# Promete

[![GitHub Releases](https://img.shields.io/github/release/ebiselutica/Promete.svg?style=for-the-badge)][releases]
[![Nuget](https://img.shields.io/nuget/v/Promete.svg?style=for-the-badge)](https://www.nuget.org/packages/Promete/)

Promete Engine is a lightweight cross-platform generic 2D game engine for C#/.NET 8.

[日本語でもご覧いただけます。](README-ja.md)

## Supported Platform

- Windows
- macOS
- GNU/Linux

## To Build

```shell
git clone https://github.com/EbiseLutica/Promete.git
cd Promete
dotnet build
```

## Features

- Lightweight processing
- 2D-specified Graphics System
	- Sprite - Display textures on the screen
	- Tilemap - Map textures on the grid
	- Graphic - Draw lines, rectangles etc
	- Container - An object which can contain other drawables
	- Text - An object which can draw text
	- 9-slice Sprite - A special sprite to split into 9 sheets to resize smoothly
- A Function to Take Screenshot
- A Feature to capture screen as a serial-numbered pictures
- Scene Management
- Keyboard Input
- Mouse Input
- Playing music
- Playing SFX
- High Extensibility
	- Add original rendering method
	- Add original audio processor

## Documents

WIP

## Contributing

Please see [Contribution Guide](CONTRIBUTING.md).

[![GitHub issues](https://img.shields.io/github/issues/EbiseLutica/Promete.svg?style=for-the-badge)][issues]
[![GitHub pull requests](https://img.shields.io/github/issues-pr/EbiseLutica/Promete.svg?style=for-the-badge)][pulls]

## LICENSE

[![License](https://img.shields.io/github/license/EbiseLutica/Promete.svg?style=for-the-badge)](LICENSE)

Promete depends on several third-party software. See these licenses: [THIRD_PARTIES.md](THIRD_PARTIES.md)

[ci]: https://ci.appveyor.com/project/EbiseLutica/Promete
[issues]: //github.com/EbiseLutica/Promete/issues
[pulls]: //github.com/EbiseLutica/Promete/pulls
[releases]: //github.com/EbiseLutica/Promete/releases
