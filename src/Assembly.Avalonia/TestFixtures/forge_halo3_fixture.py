#!/usr/bin/env python3
"""
Forges a MINIMAL, SYNTHETIC Halo 3 cache file that real Blamite can parse.

This is NOT a Halo game file and contains NO Halo assets. It is a hand-built
byte pattern whose only purpose is to drive Blamite's ThirdGen cache reader far
enough to exercise the tag-group / tag-table code paths, so a macOS UI can be
verified end to end on a machine that has no game files.

All offsets/field names come from Blamite's own layout definitions:
  src/Blamite/Formats/Halo3/Layouts/H3_Layouts_Core.xml
  src/Blamite/Formats/Engines.xml  (version 11, build 11855.07.08.20.2317.halo3_ship)
"""
import struct, sys, os

BE = '>'
HDR       = 0x3000     # header layout size for H3
TAG_BUF   = 0x3000     # 'tag buffer offset' -> also the Tag section virtual address
VIRT_BASE = 0x3000     # 'virtual base address' (== TAG_BUF so pointers == file offsets)
VIRT_SIZE = 0x10000    # 'virtual size' -- MUST be a multiple of TagSegmentAlignment
                       # (EngineDescription defaults it to 0x10000 and H3 does not override)
DEBUG_VA  = HDR + VIRT_SIZE   # 0x13000: Debug section == the string/name area
DEBUG_SIZE= 0x2000
FILE_SIZE = DEBUG_VA + DEBUG_SIZE

IDX_HDR   = 0x3000     # index header
GROUP_TBL = 0x3030     # tag group table  (0x10 per entry)
TAG_TBL   = 0x3080     # tag table        (0x08 per entry)
NAME_IDX  = 0x13000    # file name index table (int32 per entry)
NAME_DATA = 0x14000    # file name string data (asciiz)
                       # NOTE: FileSegmenter is built with SegmentAlignment (0x1000 for H3)
                       # as its default OFFSET alignment, so every segment offset must be
                       # 0x1000-aligned -- hence NAME_DATA is a page, not 0x100, after NAME_IDX.

buf = bytearray(FILE_SIZE)

def u32(off, v):  struct.pack_into(BE + 'I', buf, off, v & 0xFFFFFFFF)
def i32(off, v):  struct.pack_into(BE + 'i', buf, off, v)
def i16(off, v):  struct.pack_into(BE + 'h', buf, off, v)
def u16(off, v):  struct.pack_into(BE + 'H', buf, off, v)
def asciiz(off, s):
    b = s.encode('ascii')
    buf[off:off + len(b)] = b
    buf[off + len(b)] = 0

def fourcc(s):
    r = 0
    for ch in s.encode('ascii'):
        r = (r << 8) | ch
    return r

# ---------------- header ----------------
buf[0:4] = b'head'                                   # cache magic (big endian)
i32(0x04, 11)                                        # version -> Halo 3
u32(0x08, FILE_SIZE)                                 # file size
u32(0x10, IDX_HDR)                                   # index header address
u32(0x14, TAG_BUF)                                   # tag buffer offset
u32(0x18, VIRT_SIZE)                                 # virtual size
asciiz(0x11C, '11855.07.08.20.2317.halo3_ship')      # build string (must match exactly)
i16(0x13C, 0)                                        # type = SinglePlayer
asciiz(0x18C, 'SYNTHETIC-NOT-A-REAL-MAP')            # internal name
asciiz(0x1B0, 'levels/synthetic/poc')                # scenario name
# file table count is written after TAGS is defined (see below)
i32(0x2B8, NAME_DATA)                                # file table offset (Debug mask=0 -> absolute)
i32(0x2BC, 0x100)                                    # file table size
i32(0x2C0, NAME_IDX)                                 # file index table offset
u32(0x2E8, VIRT_BASE)                                # virtual base address
u32(0x2EC, 0)                                        # xdk version

# offset masks @0x46C, 4 x uint32 -> all zero, so pointer==offset for each section
for i in range(4):
    u32(0x46C + i * 4, 0)

# sections @0x47C, 4 x {uint32 virtual address, uint32 size}
# order: Debug, Resource, Tag, Localization
SECT = 0x47C
u32(SECT + 0 * 8 + 0, DEBUG_VA); u32(SECT + 0 * 8 + 4, DEBUG_SIZE)  # Debug (strings/names)
u32(SECT + 1 * 8 + 0, 0);        u32(SECT + 1 * 8 + 4, 0)          # Resource
u32(SECT + 2 * 8 + 0, TAG_BUF);  u32(SECT + 2 * 8 + 4, VIRT_SIZE)  # Tag
u32(SECT + 3 * 8 + 0, 0);        u32(SECT + 3 * 8 + 4, 0)          # Localization

buf[HDR - 4:HDR] = b'foot'                           # footer magic at HeaderSize-4

# ---------------- index header (0x28) ----------------
GROUPS = [
    # (magic, parent, grandparent)
    ('bipd', 'obje', -1),
    ('weap', 'item', -1),
    ('scnr', -1,     -1),
]
TAGS = [
    # (group index, meta address, name)
    (0, 0x3400, 'objects/characters/masterchief/masterchief'),
    (0, 0x3500, 'objects/characters/elite/elite'),
    (1, 0x3600, 'objects/weapons/rifle/assault_rifle/assault_rifle'),
    (1, 0x3700, 'objects/weapons/pistol/magnum/magnum'),
    (2, 0x3800, 'levels/synthetic/poc/poc'),
]

i32(IDX_HDR + 0x00, len(GROUPS))     # number of tag groups
u32(IDX_HDR + 0x04, GROUP_TBL)       # tag group table address
i32(IDX_HDR + 0x08, len(TAGS))       # number of tags
u32(IDX_HDR + 0x0C, TAG_TBL)         # tag table address
i32(IDX_HDR + 0x10, 0)               # number of global tags
u32(IDX_HDR + 0x14, 0)               # global tag table address
i32(IDX_HDR + 0x18, 0)               # number of tag interops
u32(IDX_HDR + 0x1C, 0)               # tag interop table address
i32(IDX_HDR + 0x24, fourcc('tags'))  # magic -- checked by ThirdGenTagTable.LoadHeader

# ---------------- tag group table (0x10 per entry) ----------------
for i, (magic, parent, gp) in enumerate(GROUPS):
    off = GROUP_TBL + i * 0x10
    i32(off + 0x0, fourcc(magic))
    i32(off + 0x4, fourcc(parent) if isinstance(parent, str) else parent)
    i32(off + 0x8, fourcc(gp) if isinstance(gp, str) else gp)
    u32(off + 0xC, 0)                # stringid -> 0 so GroupNames lookup is used

# ---------------- tag table (0x8 per entry) ----------------
for i, (gi, addr, _name) in enumerate(TAGS):
    off = TAG_TBL + i * 0x8
    i16(off + 0x0, gi)               # tag group index
    u16(off + 0x2, i + 1)            # datum index salt (must not be 0xFFFF)
    u32(off + 0x4, addr)             # memory address -> MetaLocation

# ---------------- tag name tables ----------------
# IndexedStringTable: index table of int32 offsets into the data blob.
i32(0x2B4, len(TAGS))   # file table count == number of tag names
cursor = 0
for i, (_gi, _addr, name) in enumerate(TAGS):
    i32(NAME_IDX + i * 4, cursor)
    b = name.encode('ascii')
    buf[NAME_DATA + cursor: NAME_DATA + cursor + len(b)] = b
    cursor += len(b) + 1            # asciiz

out = sys.argv[1]
open(out, 'wb').write(buf)
print(f"wrote {out}  {len(buf)} bytes  ({len(GROUPS)} groups, {len(TAGS)} tags)")
