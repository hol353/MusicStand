# MusicStand

## About the project

MusicStand is a desktop application for displaying sheet music on a tablet or computer. It is designed for use on a music stand during rehearsals and performances, providing a digital alternative to a folder of printed music. It displays PDF sheet music and lets musicians add annotations (freehand, highlight or musical notation stamps) with a stylus or other pointer device.

The app is being used on an Arch Linux (Wayland/GNOME/ARM) [FydeTab duo](https://fydetabduo.com/). It has been written using .NET (C#) and Avalonia which provides good cross-platform support e.g. Linux/Windows. It has only been tested on Arch Linux.

AI (Copilot/GPT) has been used to assist with specific coding tasks. Manual refactoring has then been repeatedly used to improve code maintainability. The app has not been vibe coded.

## Features

- Browse and open sheet music from a local music library.
- Display PDF pages sized to the available screen.
- Annotate music with a pen or highlighter, and erase annotations.
- Add music symbols and text stamps to a page.
- Zoom and pan around sheet music.
- Create and use setlists to organize music for a performance.
- Annotations are saved separately from the original PDF.

## Screenshots

![Light mode screenshot of home screen with a music file loaded](https://github.com/hol353/MusicStand/Docs/Img/HomeScreenshot.png)
![Dark mode screenshot of home screen with a music file loaded](https://github.com/hol353/MusicStand/Docs/Img/HomeDarkScreenshot.png)
![Screenshot of the file selection flyout](https://github.com/hol353/MusicStand/Docs/Img/FileSelectScreenshot.png)
![Screenshot of the stamp selection flyout](https://github.com/hol353/MusicStand/Docs/Img/StampScreenshot.png)

## How to build the project

Currently there are no binary releases available for download/install. Instead, you will need to manually build the application. If there is demand, binaries for different platforms may be made available.

### Requirements

- .NET 9 SDK
- A supported desktop environment for Avalonia applications
- Git

### Build and run

Clone the repository, then build and run the application from its directory:

```sh
git clone https://github.com/hol353/MusicStand.git
cd MusicStand
dotnet build MusicStand.sln
dotnet run --project MusicStand.csproj
```

The first build restores the NuGet dependencies declared by the project.

## How to contribute

Contributions are welcome. To propose a change:

1. Open an issue to describe a bug or discuss a substantial feature.
2. Fork the repository and create a branch for your change.
3. Make focused changes that follow the existing project patterns.
4. Build the solution with `dotnet build MusicStand.sln` and run the application to check the change.
5. Open a pull request with a clear summary, linking the GitHub issue.

## License

Distributed under the GPLv3 License. See LICENSE for more information.