# Assembly for macOS

Assembly's tag editor, rebuilt on [Avalonia](https://avaloniaui.net) so it runs somewhere other
than Windows. Same Blamite, same plugins, same cache files — the WPF app in `../Assembly` is
untouched and stays the reference implementation.

The point is that Blamite was always portable and the UI never was. `Blamite.csproj` multi-targets
`net48;net10.0`, so the WPF app keeps its .NET Framework build while this one consumes the exact
same library **in-process** on .NET 10. `ICacheFile`, `TagTable` and `ITag` are handed straight to
the UI — no IPC, no serialisation boundary, nothing reimplemented.

> [!NOTE]
> Not at parity with the WPF app. Real-time poking, patch creation, the script editor, the
> bitmap/model viewers and the plugin editor are all still Windows-only. What's here is opening
> caches and editing tag meta.

## running it

```bash
$ dotnet run --project src/Assembly.Avalonia
```

Use `--` before any arguments of your own, or msbuild eats them and you get a
`FileNotFoundException` for a path called `--nologo`.

## what it opens

| kind | extensions | state |
| --- | --- | --- |
| Classic caches | `.map`, `.yelo`, `.campaign` | Full meta editing, written back to disk |
| Campaign Evolved | `.utoc` + `.ucas` | Read-only — see below |
| Folders | scanned recursively | Everything recognised mounted into one namespace |
| Zips | extracted to temp | Same as a folder |

A mount is **N containers sharing one flat tag namespace**, not one open file. That distinction
exists because Campaign Evolved needs it — a mod is half a dozen separate `_P` container sets that
override each other by package ID, and retail's `Paks` folder has 28 — but it works just as well
for a folder full of classic maps.

## campaign evolved

Partial. Tags mount, parse and render. Nothing writes back yet.

CE ships its tags inside Unreal Engine 5.5 IoStore containers, and the payload in each `.ubulk`
chunk is a self-describing Halo Reach-era MCC tag file. **Self-describing is the important part**:
the payload carries its own field names, types, enum options, struct and block tables, so there are
no plugin XMLs for this engine and there should never be any. Hand-authoring 2,469 files that
duplicate what the tag already states, then re-authoring them every game patch, would be strictly
worse than reading the tag.

Almost everything known about this format was worked out from real bytes rather than documentation,
so the parser is built to disclose rather than to look confident. Sections it can't account for are
preserved verbatim and surfaced — a count in the field table, a line in the console. A tag that
opens without complaint is making a claim, and the claim is meant to be checkable.

## source structure

```
.
├── Views/          # MainWindow and friends — XAML plus the code-behind driving it
├── ViewModels/     # Tag tree, open documents, per-row edit state
├── Services/       # Mounting, cache sessions, meta read/write, plugin schema
├── Theme/          # The Metro styles ported from the WPF app
├── Assets/Fonts/   # Selawik — see gotchas
├── HeadlessProbe.cs
└── README.md       # 📍 You are here
```

## headless mode

The window isn't the only way in. `HeadlessProbe` drives the same services from a terminal, which
is how the Campaign Evolved work got verified without clicking anything:

```bash
# open a cache, print what mounted
$ dotnet run --project src/Assembly.Avalonia -- --headless <cache-file> [tag-name-substring]

# open, edit a field, save, reopen, prove the new bytes are on disk
$ dotnet run --project src/Assembly.Avalonia -- --edit-test <cache-file> <tag> <field> <value>
```

`--edit-test` goes through the same `TagDocumentViewModel` and `MetaValueWriter` the sidebar
editors use, so it proves the real write path rather than a parallel one.

`ASM_SHOT=<path>` screenshots the window on open, which is handy for iterating on layout.

## gotchas

- **`Engines.xml` resolves its nested paths against the process working directory**, with no
  rebasing. Anything loading the engine database has to `SetCurrentDirectory` to the output folder
  first. `EngineDatabaseService` does it — copy that shim if you add another entry point.
- **Windows-1252 isn't in the box outside .NET Framework.** Register `CodePagesEncodingProvider`
  before any string read or Blamite throws on macOS and Linux.
- **Native window chrome is deliberate.** The WPF app draws its own borderless titlebar; this one
  doesn't. On macOS a custom titlebar fights window management, Spaces and VoiceOver for nothing,
  and VS Code — the thing people mean by "VS Code ergonomics" — uses native chrome here too.
  Everything below the titlebar is a faithful port.
- **Plugins are linked, not copied.** The tag-definition XML tree lives in `../Assembly/Plugins`
  and is `<Content Include>`d from there so both front-ends read one source of truth. It should
  probably move to a shared content project.
- **The font is Selawik, not Segoe.** Assembly's Metro theme wants Segoe WP, which is
  Microsoft-internal and not redistributable. Selawik is Microsoft's own open-source metric
  substitute (OFL 1.1) and its five weights map 1:1 onto the five `MetroFont*` resources.
- **WPF triggers don't port mechanically.** `<ControlTemplate.Triggers>` and
  `<Trigger Property="IsMouseOver">` have no Avalonia equivalent and become pseudo-class selectors
  (`Button:pointerover`, `:pressed`, `:selected`). The rewrite is per-template and is most of the
  work left in porting the remaining Metro controls.
- **Campaign Evolved containers load eagerly.** Fine for a five-tag mod. Unproven against retail's
  ~12,000, because nobody working on this has the retail containers.
- **The Oodle decompressor is unexercised.** Mod containers store everything uncompressed, so the
  pure-managed Kraken decoder has never had real shipped-game data through it.
- **`TestFixtures/forge_halo3_fixture.py` is orphaned.** It still writes a synthetic Halo 3 cache,
  but the in-app "SYNTHETIC TEST FIXTURE" banner that made such a file impossible to mistake for a
  real map is gone. Don't rely on it until that's back.

## tests

```bash
$ dotnet test src/Blamite.Tests
```

Format-level tests live with Blamite. Roughly two thirds need a real Campaign Evolved container set
and skip by name when it's absent — see [`../Blamite.Tests/README.md`](../Blamite.Tests/README.md).
