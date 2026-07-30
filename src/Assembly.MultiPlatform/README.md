# Assembly, everywhere else

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
> caches, editing tag meta, and packing Campaign Evolved containers.

## running it

Paths below are relative to this directory.

```bash
$ dotnet run
```

Use `--` before any arguments of your own, or msbuild eats them and you get a
`FileNotFoundException` for a path called `--nologo`.

For a real macOS app — Dock icon, bundle identity, double-clicking a `.utoc` in Finder:

```bash
$ dotnet publish -c Release -r osx-arm64 -p:SelfContained=true
$ open bin/Release/net10.0/osx-arm64/publish/Assembly.app
```

The bundle is unsigned, so Finder will refuse it on first launch until you either sign it or clear
it in *System Settings → Privacy & Security*. `open` from a terminal is the usual way round that
while developing. `BundleApp` hangs off `Publish` rather than `Build` deliberately — assembling a
bundle on every inner-loop compile would be latency for nobody's benefit.

## what it opens

| kind | extensions | state |
| --- | --- | --- |
| Classic caches | `.map`, `.yelo`, `.campaign` | Full meta editing, written back to disk |
| Campaign Evolved | `.utoc` + `.ucas` | Meta editing, written back into the container |
| Folders | scanned recursively | Everything recognised mounted into one namespace |
| Zips | extracted to temp | Same as a folder |

A mount is **N containers sharing one flat tag namespace**, not one open file. That distinction
exists because Campaign Evolved needs it — a mod is half a dozen separate `_P` container sets that
override each other by package ID, and retail's `Paks` folder has 28 — but it works just as well
for a folder full of classic maps.

## campaign evolved

CE ships its tags inside Unreal Engine 5.5 IoStore containers, and the payload in each `.ubulk`
chunk is a self-describing Halo Reach-era MCC tag file. **Self-describing is the important part**:
the payload carries its own field names, types, enum options, struct and block tables, so there are
no plugin XMLs for this engine and there should never be any. Hand-authoring 2,469 files that
duplicate what the tag already states, then re-authoring them every game patch, would be strictly
worse than reading the tag.

The full loop works — mount, parse, edit, save, and the bytes land back in the `.ucas`. Writing is
built to be **byte-exact**: an unedited tag is returned as an exact clone of what was read, and an
edited one is rebuilt chunk by chunk with every undirtied chunk replayed verbatim. That is not
belt-and-braces. Two things genuinely cannot be recovered from the decoded model — whether a
stringID's text section was NUL-terminated (real data has both, 165 of 171 unterminated), and
whether an elided struct wrapper was written as zero bytes — so anything short of replaying the
original bytes would quietly corrupt tags it had merely *read*.

**Unpack** and **Repack** on the toolbar extract a container set's tags to a folder of loose files
and build a container set back from one, which is the workflow for editing outside Assembly or
diffing two versions of a mod.

Almost everything known about this format was worked out from real bytes rather than documentation,
so the parser is built to disclose rather than to look confident. Sections it can't account for are
preserved verbatim and surfaced — a count in the field table, a line in the console. A tag that
opens without complaint is making a claim, and the claim is meant to be checkable.

## keyboard

Every command has a key, and every key is in one place: **⌘K** opens the command palette, which
lists all of them with their bindings and is fuzzy-searchable. **⌘/** shows the full sheet.

| | |
| --- | --- |
| `⌘K` | Command palette |
| `⌘P` | Go to tag |
| `⌘F` / `⌘⇧F` | Focus the tag filter / the field filter |
| `⌘[` / `⌘]` | Back / forward through visited tags |
| `⌘\` | Toggle tag tree (`⌘⇧\` properties, `⌘⌥\` console) |
| `⌘O` / `⌘⇧O` | Open cache file / folder |
| `⌘S` / `⌘W` | Save / close tab |

On Windows and Linux these are the same bindings with Ctrl — `Services/PlatformKeys.cs` resolves
the primary modifier once so nothing hardcodes one. The macOS menu bar is a real `NativeMenu`, not
a drawn strip, so these show up in it and in *Help → Search* like any native app's.

## density

Compact, Default and Comfortable, from the toolbar or the palette. Density drives row height,
control height and the type and spacing scales together, so it stays a coherent layout rather than
just smaller text — `Services/DisplayDensity.cs` documents exactly what does and doesn't reflow.
The choice persists; `ASM_DENSITY` overrides it for one run without touching the saved preference.

## source structure

```
.
├── Views/          # MainWindow and friends — XAML plus the code-behind driving it
│   └── Editors/    # One per field kind: enum, flags, colour, tag reference, hex…
├── ViewModels/     # Tag tree, open documents, per-row edit state
├── Services/       # Mounting, cache sessions, meta read/write, packing, commands
├── Theme/          # Metro palette, type scale, icon geometry — see below
├── Assets/Fonts/   # Selawik — see gotchas
├── Assets/Icon/    # Assembly.svg is the source; the .icns and .png are rendered from it
├── Info.plist      # Bundle metadata template — BundleApp substitutes the version
├── HeadlessProbe.cs
└── README.md       # 📍 You are here
```

## theming

`MetroDark.axaml` carries the WPF Metro palette across — the greys from
`src/Assembly/Metro/Themes/Dark.xaml` and the `#0079cb` accent from `Blue.xaml`, unchanged — now
as the Dark half of a `ResourceDictionary.ThemeDictionaries` with a Light half beside it. Light
deepens the state colours that don't clear WCAG contrast on a white surface; that file's header
shows the arithmetic.

The app follows the OS light/dark setting. `ASM_THEME=Light|Dark` pins one for testing.

`MetroTypography.axaml` holds the type scale and an 8px spacing scale; `Icons.axaml` holds
hand-authored `StreamGeometry` for the chrome icons that used to be bare Unicode glyphs. Both are
theme-invariant, hence separate files from the palette.

One accent, and it means something. Colour is spent on state that's worth interrupting someone for
— unsaved changes, a failed lookup, a selected row — and the permanent furniture stays grey. An
indicator that is lit in every frame of the app's life isn't an indicator.

## headless mode

The window isn't the only way in. `HeadlessProbe` drives the same services from a terminal, which
is how the Campaign Evolved work got verified without clicking anything:

```bash
# open a cache, print what mounted
$ dotnet run -- --headless <cache-file> [tag-name-substring]

# every field of a CE tag, with the editor each one resolves to
$ dotnet run -- --ce-fields <utoc-or-folder> <tag-name-substring>

# open, edit a field, save, reopen, prove the new bytes are on disk
$ dotnet run -- --edit-test <cache-file> <tag> <field> <value>

# read every tag, re-encode it untouched, diff against the original
$ dotnet run -- --fifthgen-roundtrip <utoc>
```

`--edit-test` goes through the same `TagDocumentViewModel` and `MetaValueWriter` the sidebar
editors use, so it proves the real write path rather than a parallel one. `Program.cs` keeps the
list of headless modes in a `HashSet` — add yours there as well as to `HeadlessProbe`, or it falls
through and launches the window instead.

`ASM_SHOT=<path>` screenshots the window on open, which is how the layout work gets checked.

## gotchas

- **`Engines.xml` resolves its nested paths against the process working directory**, with no
  rebasing. Anything loading the engine database has to `SetCurrentDirectory` to the output folder
  first. `EngineDatabaseService` does it — copy that shim if you add another entry point.
- **Windows-1252 isn't in the box outside .NET Framework.** Register `CodePagesEncodingProvider`
  before any string read or Blamite throws on macOS and Linux.
- **`<AssemblyName>` is the macOS menu-bar name.** AppKit titles the application menu from
  `CFBundleName` inside a bundle and from the executable's file name outside one — and `dotnet run`
  is outside one. Both say `Assembly`; change one and change the other.
- **Native window chrome is deliberate.** The WPF app draws its own borderless titlebar; this one
  doesn't. On macOS a custom titlebar fights window management, Spaces and VoiceOver for nothing,
  and VS Code — the thing people mean by "VS Code ergonomics" — uses native chrome here too.
  Everything below the titlebar is a faithful port.
- **A Style Setter can never beat a locally-set property.** Avalonia has no WPF-style value
  precedence escape hatch here, so `FontSize="12"` in a template makes that element permanently
  immune to the type scale and to density. Use `{DynamicResource}` in the template instead; this
  has been the cause of more than one "why won't this restyle" afternoon.
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

## tests

```bash
$ dotnet test ../Blamite.Tests
```

Format-level tests live with Blamite. Most of them need a real Campaign Evolved container set and
skip by name when it's absent — see [`../Blamite.Tests/README.md`](../Blamite.Tests/README.md).
