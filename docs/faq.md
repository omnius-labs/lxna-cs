## UI scaling is wrong on Linux

You need to set the AVALONIA_SCREEN_SCALE_FACTORS environment variable.

For debugging, you can set environment variables in the .env file with the following command.

```sh
SCALE=2 bash ./eng/gen-envfile.sh
```

### Refs
- https://github.com/AvaloniaUI/Avalonia/issues/4826
- https://github.com/AvaloniaUI/Avalonia/issues/6923

## System.InvalidOperationException: Default font family name can't be null or empty.

You need to set the LANG=en_US.UTF-8 environment variable.

For debugging, you can set environment variables in the .env file with the following command.

```sh
SCALE=2 bash ./eng/gen-envfile.sh
```

### Refs
- https://github.com/AvaloniaUI/Avalonia/issues/4427
