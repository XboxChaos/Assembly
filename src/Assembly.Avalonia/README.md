# Assembly (Avalonia)

A cross-platform front-end for Assembly, targeting macOS, Linux and Windows.

It consumes `src/Blamite` **in-process**, exactly as the WPF app does — there is no
IPC layer and no serialization boundary. `ICacheFile`, `TagTable`, `ITag` and the
rest of Blamite's object model are handed straight to the UI.

**No changes to Blamite are required, and none have been made.**

## Status

Early. The tag browser and a read-only meta viewer work. This does not yet replace
`src/Assembly` (the WPF app), which remains the full-featured, Windows-only client.

| Area | State |
| --- | --- |
| Open cache file, engine auto-detection | working |
| Tag hierarchy — by tag group | working |
| Tag hierarchy — by tag-name folder path | working |
| Tag/group filter | working |
| Tag meta viewer (plugin-driven, read-only) | working |
| Enums, bitfields, tag refs, ranges, colours, string IDs | working |
| Nested tag blocks | shown with header + indented child fields; entries are not yet walked |
| Meta **editing** / saving | not started |
| Script, string, locale, sound, BSP editors | not started |
| Docking / multiple tabs | not started |
| Real-time editing (RTE), poking | not started — Windows/Xbox only in Blamite |

## Running

```
dotnet run --project src/Assembly.Avalonia
```

Headless smoke test (no window):

```
dotnet run --project src/Assembly.Avalonia -- --headless <cache.map> [tag-name-substring]
```

## Two platform notes

Both are handled in `Services/EngineDatabaseService.cs`; neither required a Blamite change.

1. **Working directory.** Blamite resolves the nested database paths inside
   `Formats/Engines.xml` against the process current directory with no rebasing, so the
   app sets the CWD to its own base directory at startup. The WPF app does the same in
   `App.xaml.cs`.
2. **Windows-1252.** `EndianReader.ReadWin1252` needs a code page that .NET does not
   register by default outside .NET Framework. The app registers
   `CodePagesEncodingProvider` before any cache is opened, or string reads throw on
   macOS and Linux.

## Test fixtures

There are usually no Halo game files on a CI machine or a fresh dev box.
`TestFixtures/forge_halo3_fixture.py` writes a tiny **synthetic** Halo 3 cache file —
a hand-built byte pattern containing no Halo assets — that real Blamite parses far
enough to exercise engine detection, header parsing, the tag group table, the tag
table and tag names.

```
python3 src/Assembly.Avalonia/TestFixtures/forge_halo3_fixture.py /tmp/fixture.map
dotnet run --project src/Assembly.Avalonia -- --headless /tmp/fixture.map masterchief
```

The app detects these files (via their internal name) and shows a red
"SYNTHETIC TEST FIXTURE" banner, so a fixture can never be mistaken for a real map.

## Theming

`Theme/MetroDark.axaml` carries the WPF Metro palette across verbatim — the greys from
`src/Assembly/Metro/Themes/Dark.xaml` and the `#0079cb` accent from `Blue.xaml`.

`Theme/MetroStyles.axaml` ports the control styling. The one systematic difference from
WPF: `<ControlTemplate.Triggers>` / `<Trigger Property="IsMouseOver">` have no Avalonia
equivalent and become pseudo-class selectors (`Button:pointerover`, `:pressed`,
`:selected`). That rewrite is mechanical but it is per-template, and it is the bulk of
the work in porting the remaining Metro control templates.
