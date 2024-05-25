# Lxna - Omnius File Manager (Desktop app)

[![test](https://github.com/omnius-labs/lxna-cs/actions/workflows/test.yml/badge.svg?branch=main)](https://github.com/omnius-labs/lxna-cs/actions/workflows/test.yml)
[![chat](https://badges.gitter.im/omnius-labs.svg)](https://gitter.im/omnius-labs/community)

Lxna is a desktop file manager developed by Omnius Labs, optimized for managing and previewing images and video files with a user-friendly interface.

![Demo](/docs/images/demo.png)

## Features

- **Folder Tree View**: Easily select folders through the folder tree view.
- **Thumbnail Preview**: Double-click on files in the thumbnail list to open an enlarged preview.
- **Auto-Update**: The application features an auto-update function, ensuring you always have access to the latest features.
- **Display Thumbnails for Images**: Thumbnails for image files are displayed.
- **Rotating Thumbnails for Videos**: Generates multiple thumbnails for video files and displays them in a rotating fashion for quick browsing (referred to as "Dynamic Thumbnail Preview").

## Getting Started

### Installation Requirements

Before installing Lxna, ensure your system has the necessary dependencies installed:

#### Debian and Ubuntu

```sh
sudo apt-get install -y ffmpeg
```

### Windows

```sh
scoop install ffmpeg
```

## Installation

Download the latest release from the link below.

- [Download for Windows and Linux](https://github.com/omnius-labs/lxna-cs/releases)

## Launching the Application

### Debian and Ubuntu

To launch the application on Debian and Ubuntu, set the necessary environment variables as follows:

```sh
export LANG=en_US.UTF-8
SCALE=2
SCREEN=$(xrandr --listactivemonitors | awk -F " " '{ printf("%s", $4) }')
export AVALONIA_SCREEN_SCALE_FACTORS="$SCREEN=$SCALE"
```

## How to Usage

1. Select Folder: Use the folder tree view to select the folder you want to manage.
2. Preview Files: Double-click a file in the thumbnail view to display a preview of the file.

## Documentation

- [Requirements](./docs/requirements/index.adoc)
- [Specifications](./docs/specifications/index.adoc)
- [FAQ](./docs/faq.md)

## Links

- Official Documentation: https://docs.omnius-labs.com/
- Icons provided by: https://icooon-mono.com/

## License

This project is released under the MIT License. For more details, please refer to the [LICENSE](LICENSE.txt) file.

## Contributing

If you would like to contribute to this project, please contact us through [Issues](https://github.com/lyrise/image-classifier-cs/issues) or [Pull Requests](https://github.com/lyrise/image-classifier-cs/pulls) on GitHub.
