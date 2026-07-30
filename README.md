# Assembly

### Multi-Generation Blam Engine Research Tool ###

__An Xbox/Xbox 360 console capable of running unsigned code is required, for console-based games, in order to use modifications created with Assembly. Flashed disc drives will not work.__

__An Xbox/Xbox 360 console capable of running a dev kernel is required, for console-based games on their intended platform, in order to modify or interact with a game or console in real time (poking) with Assembly.__

Assembly is a free, open-source Halo cache file (.map) editor that was built from the ground up. It allows users to create and distribute creative patches for game content.

Assembly was designed with three goals in mind: 

* __Flexibility__ - Assembly is capable of opening files targeted for Halo 1, Halo 2, Halo 3, Halo: Reach, and Halo 4, and Halo MCC, with partial support for Halo: Campaign Evolved. And includes a system which allows users to add in support for other formats with ease.
* __Speed__ - Spend more time researching and less time waiting for trivial tasks to complete. Even the largest tags load extremely quickly with invisible fields enabled, and the meta editor's search feature allows users to find values with ease.
* __Usability__ - Built using Windows Presentation Foundation and utilizing modern UI design concepts, Assembly is both easy to use and easy to look at.

## Halo: Campaign Evolved ##

Support for Campaign Evolved is __partial__. Its tags can be mounted, parsed and viewed, and an edited tag can be written back into its container. Editing them through the user interface is still being wired up.

Campaign Evolved does not ship cache files. It ships its tags inside Unreal Engine 5.5 IoStore containers (`.utoc`/`.ucas`), and the tag inside each one is self-describing - it carries its own field names, types, enumeration options and block definitions. That means it needs no plugins, and none should be written for it: a hand-maintained copy of what the tag already states would only drift on every game patch.

Handing Assembly any one `.utoc` mounts every container beside it and resolves overrides across the lot, because a Campaign Evolved mod is several separate container sets rather than one file.

## macOS and Linux ##

The Windows client is Windows Presentation Foundation and stays that way. There is also an [Avalonia front-end](src/Assembly.Avalonia/README.md) which runs on macOS, Linux and Windows, and which consumes the same Blamite library in-process. It opens cache files and edits tag meta; it is not at parity with the Windows client, and real-time editing, patch creation and the specialised editors remain Windows-only.

## Downloading ##

Stable releases are made available through [GitHub's release system](https://github.com/XboxChaos/Assembly/releases).

## Precompiled Builds ##

At this time precompiled builds should not be shared unless by a [team member](https://github.com/orgs/XboxChaos/people).

## Compiling ##

See [Compiling Assembly from Source](https://github.com/XboxChaos/Assembly/wiki/Compiling-from-Source).

## Bug Reports ##

Assembly isn't perfect. If you encounter any issues, you are encouraged to submit bug reports through our [issue tracker](https://github.com/XboxChaos/Assembly/issues/new). Please make your reports as detailed as possible. Be sure to include any exception messages you get (if any), what map the error occurred on, and give steps showing how we can reproduce the behavior you encountered.
