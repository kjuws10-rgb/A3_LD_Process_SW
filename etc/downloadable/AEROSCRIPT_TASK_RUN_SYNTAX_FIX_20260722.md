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
    var $StartYPos as real

    $StartYPos = 500
    MoveAbsolute(Y, $StartYPos, 20)
    G90 G0 GX 0 GY 0
    WaitForInPosition(Y)
end
```

- Variables are declared inside the `program` block.
- `MoveAbsolute(axis, position, speed)` is used for the variable-based Stage move.
- `MoveAbsolute` is asynchronous, so `WaitForInPosition(Y)` remains required.
- Generated source is rejected if it contains a G-code operand such as `Y $variable`.
- Equipment mode emits literal axis arrays such as `MoveLinear([GX, GY], [x, y], speed)`.

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

