# Lxna - Omnius File Manager (Desktop app)

[![test](https://github.com/omnius-labs/lxna-cs/actions/workflows/test.yml/badge.svg?branch=main)](https://github.com/omnius-labs/lxna-cs/actions/workflows/test.yml)
[![chat](https://badges.gitter.im/omnius-labs.svg)](https://gitter.im/omnius-labs/community)

## Installing required dependencies

### Debian and Ubuntu

```sh
sudo apt-get install -y ffmpeg
```

### Windows

```sh
scoop install ffmpeg
```

## Launching the Application

### Debian and Ubuntu

To launch the application on Debian and Ubuntu, set the necessary environment variables as follows:

```sh
export LANG=en_US.UTF-8
SCALE=2
SCREEN=$(xrandr --listactivemonitors | awk -F " " '{ printf("%s", $4) }')
export AVALONIA_SCREEN_SCALE_FACTORS="$SCREEN=$SCALE"
```

## Documentation

- [Requirements](./docs/requirements/index.adoc)
- [Specifications](./docs/specifications/index.adoc)
- [FAQ](./docs/faq.md)

## Links

- docs: https://docs.omnius-labs.com/
- icons: https://icooon-mono.com/
