EventItemBag - quick guide
============================================================

This folder is NOT read by GameServer (the loader ignores
subfolders, it only reads loose files in Data/EventItemBag/).
It's just reference material for you.

Each real bag lives at Data/EventItemBag/NNN - Name.xml, where NNN
is the same number as its <Bag Index="NNN"> in EventItemBagManager.xml.
You can freely organize these into subfolders (by system, category,
whatever makes sense to you) - the loader scans every subfolder,
any depth. Only the leading number in the filename matters; the
folder it's in is purely for your own organization.

A bag file can have TWO reward systems:

  <Item>  basic format     - simple list, weighted per item (DropRate)
  <Ex>    extended format  - several independent drops, with
                             per-class restrictions

Which one is actually used is controlled by the UseEx attribute on
the root of the file:

    <ItemBag UseEx="0"> ... </ItemBag>   -> uses <Item> (default if
                                             the attribute is absent)
    <ItemBag UseEx="1"> ... </ItemBag>   -> uses <Ex>

You can write BOTH in the same file (one just goes unused, no harm
done) and switch between them just by flipping UseEx - no need to
delete anything.

See:
  - "01 - Basic Format.xml"    full commented example of <Item>
  - "02 - Extended Format.xml" full commented example of <Ex>

To edit a real bag, copy whichever part you need from these
examples into Data/EventItemBag/NNN - Name.xml and adjust the values.
