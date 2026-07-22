# AeroScript Task Run Syntax Fix

## Problem

The generated Simulation script used this line:

```aeroscript
G90 G0 Y $StartYPos
```

Automation1 parses a G-code axis operand and its variable as one token. The official
G-code form is `Y$StartYPos`, without a space. The previous line therefore stopped
at line 39 with `unexpected identifier` and also reported `$StartYPos` as unused.

## Applied Fix

The generator now emits an explicit AeroScript motion function:

```aeroscript
program
    var $StageAxisName as string
    var $StageAxis as axis
    var $StartYPos as real

    $StageAxisName = "Y"
    $StageAxis = @$StageAxisName
    $StartYPos = 500
    MoveAbsolute($StageAxis, $StartYPos, 20)
    WaitForInPosition($StageAxis)
end
```

- Variables are declared inside the `program` block.
- Axis names are converted from strings with the official `@` operator.
- `MoveAbsolute(axis, position, speed)` is used with an `axis` variable.
- `MoveAbsolute` is asynchronous, so `WaitForInPosition($StageAxis)` remains required.
- Generated source is rejected if it contains a G-code operand such as `Y $variable`.
- Equipment mode emits literal axis arrays such as `MoveLinear([GX, GY], [x, y], speed)`.

## Stale Output Prevention

`mof_generated.ascript` is now included in the project and copied to the output
directory with `CopyToOutputDirectory=Always`. Every build therefore replaces a
legacy file left under `bin/Debug/net8.0-windows`. Runtime generation also writes a
generator revision marker and reads the saved file back to verify exact content.

## Dynamic Axis Resolution

`unexpected ',', expecting '.' or '('` at `MoveAbsolute(Y, ...)` means the active
compiler configuration did not resolve `Y` as an axis value. Simulation V4 stores
`Y`, `GX` and `GY` as strings, converts them with `@$AxisName`, and uses only axis
variables in motion and status functions. The WPF direct-client preflight still
checks that those names exist and are virtual before Controller compilation.

## Simulation And Equipment Boundary

Simulation mode excludes Laser, PSO, Hardware Aux and Galvo calibration commands.
It validates virtual Y/GX/GY motion and Stage PositionFeedback wait release only.

Equipment mode still requires the validated MCD, physical axes, homing and limits,
Safety Interlock, Laser/Beam Path checks and final operator approval. The supplied
hardware samples are references; machine-specific Laser/PSO settings are not copied
automatically into generated production scripts.

## UI Log

Double-click the deployment log area to clear all displayed log lines. This affects
only the UI text and does not delete Controller audit files or local AeroScript files.
