# Libs

Have experimented a couple of times with adding the source to this project. Didn't work fully by adding package references, because they needed to be in the IDE. But they didn't support the IDE. Maybe instead there could be a build script?

## Adding submodules

```
git submodule add https://github.com/anegostudios/vsapi.git VintagestoryAPI
git submodule add https://github.com/anegostudios/vscreativemod.git VSCreativeMod
git submodule add https://github.com/anegostudios/vssurvivalmod.git VSSurvivalMod
git submodule add https://github.com/anegostudios/vsessentialsmod.git VSEssentialsMod
git submodule add https://github.com/anegostudios/Cairo.git Cairo
```

## Updating submodules

```
python3 update.py 1.21.6 --yes
```