# Blamite.Tests

Regression tests for Blamite's file-format code. Written for the Campaign Evolved work, where
almost everything the parser knows was established by reading real bytes rather than from
published documentation — which means nothing upstream will tell us when a refactor quietly
changes what a tag parses into.

Run from this directory:

```bash
$ dotnet test
```

## The three parts

**`IoStorePrimitiveTests`** builds its buffers by hand and needs no game files, so it runs
anywhere — including on a machine that has never seen the game. It pins the byte-level rules
that were most expensive to work out and would be easiest to silently undo: the mixed-endian
`FIoChunkId`, the 40-bit offset packed alongside a 24-bit size in a compressed-block entry, and
the fact that every four-CC in a tag payload is stored backwards.

**`CampaignEvolvedTests`** needs a real `.utoc`/`.ucas` container set and covers the whole path:
engine detection, mounting every sibling container from one file, override resolution, tag naming,
and payload parsing. It opens everything read-only.

**`CampaignEvolvedWriterTests`** pins the property the write path rests on: an unedited tag
serialises back to the bytes it was parsed from, exactly. That is the mechanism, not a nicety —
two things are unrecoverable from the decoded model (whether a stringID or tag-reference text
section was NUL-terminated on disk, and whether a struct field's wrapper was elided to zero
bytes), and the writer sidesteps both by replaying anything undirtied verbatim. A regression means
it has begun inventing bytes, which no round-trip of its own output would reveal.

Container repack is deliberately not covered here. It writes files, and the only real container
set available is a third-party mod these tests must not touch.

## Test data

The reference container set is a third-party mod. It is deliberately **not** vendored here — it
is not ours to redistribute, and it is four megabytes. Tests that need it skip by name, with an
explanation, rather than passing vacuously:

```
Passed!  - Failed: 0, Passed: 13, Skipped: 35, Total: 48
```

Point `ASM_CE_TEST_DATA` at any directory containing a `.utoc`/`.ucas` pair to run them. The
default is `~/Downloads/Flyable Pelican July 29 2026`, which is where the set the expected values
were measured from happened to live.

## Expected values are measurements, not aspirations

The per-tag numbers in `CampaignEvolvedTests.Expectations` — payload sizes, field and struct
counts, and **warning counts** — are what the parser actually produced at the point CE support
was first proven end-to-end.

The warning count deserves a note, because zero would be the wrong number to encode. Real
payloads contain sections nobody has decoded, and this parser is built to say so rather than to
hide them; a tag that parses "cleanly" would more likely mean the disclosure broke than that the
data got simpler. Pinning the count catches a new warning appearing *and* a known one
disappearing.

So a failure here is not automatically a bug. It means the parser's behaviour changed, and
someone has to decide which of the two numbers is now wrong.
