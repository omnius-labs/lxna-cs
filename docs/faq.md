## UI scaling is wrong on Linux

You need to set the AVALONIA_SCREEN_SCALE_FACTORS environment variable.

You can set environment variables in the .env file with the following command.

```sh
SCALE=2
SCREEN=$(xrandr --listactivemonitors | awk -F " " '{ printf("%s", $4) }')
export AVALONIA_SCREEN_SCALE_FACTORS="$SCREEN=$SCALE"
```

### Refs
- https://github.com/AvaloniaUI/Avalonia/issues/4826
- https://github.com/AvaloniaUI/Avalonia/issues/6923

## System.InvalidOperationException: Default font family name can't be null or empty.

You need to set the LANG=en_US.UTF-8 environment variable.

You can set environment variables in the .env file with the following command.

```sh
export LANG=en_US.UTF-8
```

### Refs
- https://github.com/AvaloniaUI/Avalonia/issues/4427
