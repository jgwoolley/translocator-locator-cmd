# Create Mod Images

These are some scripts that will generate VintageStory modicons as well as Screenshots in the correct resolution for ModDB.

You should just be able to cd into this folder and run the following docker command to update the images from the base svg:

```console
$ pwd
.../translocator-finder-cmd/CreateImages
$ run.sh
```

When you are completely done, you can remove unused docker images / volumes by running the following command:

```console
docker system prune -a --volumes
```