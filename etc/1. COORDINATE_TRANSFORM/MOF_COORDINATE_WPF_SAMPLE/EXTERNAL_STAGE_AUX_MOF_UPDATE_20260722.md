# External Stage AUX MOF Update V5

## 2026-07-23 Field Script Alignment V9

The generated equipment script now follows the field script concept shared from the line:

- Stage Y is a third-party axis and is not commanded by Automation1.
- Stage encoder resolution defaults to `16000 counts/mm`.
- The scanner follows the Stage until the configured `Follow Before Process mm` distance. Default: `200 mm`.
- Pre-follow commands are generated as 10 mm steps:

```aeroscript
MoveRapid([GY,GX], [-10,0])
wait(StatusGetAxisItem(GY, AxisStatusItem.AuxiliaryFeedback) > $encoder + (160000))
...
MoveRapid([GY,GX], [-200,0])
wait(StatusGetAxisItem(GY, AxisStatusItem.AuxiliaryFeedback) > $encoder + (3200000))
```

- Process commands then use generated scanner coordinates:

```aeroscript
MoveRapid([GY,GX], [<process GY>, <process GX>])
wait(StatusGetAxisItem(GY, AxisStatusItem.AuxiliaryFeedback) > $encoder + (<stage travel count>))
GalvoLaserOutput(GY, GalvoLaser.On)
MoveDelay([GY,GX], <shot delay>)
GalvoLaserOutput(GY, GalvoLaser.Off)
```

Important distinction: `GY/GX` are scanner process coordinates. The `wait(...)` threshold is external Stage travel converted to encoder counts. These values are related by the MOF concept but are not the same coordinate.

When multiple scanners are selected, the WPF client creates one script package per scanner and assigns consecutive Automation1 Tasks. This avoids mixing multiple scanner heads into one AeroScript Task and lets Automation1 run them in parallel.

## Equipment Model

The board Stage is controlled by a third-party motion controller. Automation1 does not contain or command a Stage Y axis. The physical Stage encoder is wired to the auxiliary encoder input associated with each scanner GY axis.

```text
Third-party Stage controller -> Stage motor
Stage encoder A/B            -> GL4/GI4 scanner AUX input
Automation1 GY AUX counter   -> MOF compensation and wait gates
```

## Generated Sequence

1. Wait for GX/GY to be in position.
2. Disable the previous MOF scale with `GalvoEncoderScaleFactorSet(GY, 0)`.
3. Zero the GY auxiliary feedback counter.
4. Calculate `scanner counts per unit / external encoder counts per unit`.
5. Apply direction sign and enable MOF compensation.
6. Wait until `Abs(AuxiliaryFeedback)` crosses each configured travel threshold.
7. Execute scanner GX/GY commands in board-local AK1-to-AK2 order.
8. Wait for scanner motion completion and reset the MOF scale to zero.

The script writes current travel and command progress to `$rglobal[0]`, `$iglobal[0]`, and `$iglobal[1]` so the WPF monitor can show the active cell without redrawing the full board.

No `MoveAbsolute(Y, ...)`, `WaitForInPosition(Y)`, or Stage `PositionFeedback` command is generated in this mode.

## Parameters

- `External Encoder cnt/mm`: physical Stage encoder counts received for 1 mm of travel. Default sample value is 2000.
- `Encoder Direction`: `-1` when scanner compensation and Stage encoder directions are opposite, otherwise `1`.
- `AUX Initial Wait mm`: minimum encoder travel after AUX zeroing before the first processing group is released.
- `Stage Travel`: validation-only expected physical travel. The script does not command this movement.

## Simulation Boundary

Automation1 Virtual mode cannot reproduce the physical AUX input, laser output, or other hardware signals. Use `Simulation - Virtual Wait` only for virtual-axis sequencing. Use `Equipment - External Stage AUX MOF` only after encoder wiring, counts/unit, direction, scanner scale, interlocks, and laser-disabled dry-run checks are complete.

## Compile Result

The Automation1 .NET compiler returns a `CompiledAeroScript` object and does not automatically create a Controller file. The client now writes `CompiledBytes` to the source stem with `.a1exe`, for example:

```text
programs/mof_generated.ascript
programs/mof_generated.a1exe
```

When job preservation is enabled, both files use the same timestamp/job suffix.

## Duplicate Axis Names

Some runtime configurations expose names that differ only by case, such as `z` and `Z`. The client now groups those entries with an ordinal-ignore-case comparer and uses the first runtime axis for validation, avoiding the previous duplicate dictionary key exception.
