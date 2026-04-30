---
name: reeutils
description: "Grep, ls, find, tree, analyze, inspect data in RE4R game files (.pak, .scn, .user, .msg) via an RE4R pak file. Outputs folders, game objects, components, guids, properties, and values."
---

Use `reeutils` to examine RE4R game files
Assume it is on PATH, if not, ASK THE USER to provide the path to the executable or ensure it is on PATH.

If the user has specified an RE4R install path, use `re_chunk_000.pak` as the pak filename.
If the user has not specified an RE4R install path or pak path, ASK THE USER FOR IT.

## Commands

Listing RE4R game files:

- reeutils ls -g re4 --pak path/to/file.pak PATH
- reeutils find -g re4 --pak path/to/file.pak PATH_PATTERN... PATH_PATTERN...

Searching RE4R game files for strings and guids:

- reeutils grep -g re4 --pak path/to/file.pak -r REGEX_PATTERN PATH... PATH...

Viewing contents of RE4R game files:

- reeutils tree -g re4 --pak path/to/file.pak natives/stm/leveldesign/chapter/chap3_01/chap3_01_level.scn.21
- reeutils tree -g re4 --pak path/to/file.pak natives/stm/leveldesign/chapter/chap3_01/chap3_01_level.scn.21 GAME_OBJECT_PATH|GAME_OBJECT_GUID...
- reeutils tree -g re4 --pak path/to/file.pak natives/stm/leveldesign/chapter/chap3_01/chap3_01_level.scn.21 3d6f4cf1-6ba6-49b6-b43e-749269c22ef5 LevelPlayerCreateController

## Workflow

- Use ls, find, or grep to identify relevant files
- Use tree to get the details in a file
- LAST RESORT grep near the root of the pak natives/stm/.../, this will be slow. There will many files to search.
- DO NOT USE hierarchy or inspect for ordinary browsing of game files, these are for getting a dependency tree
