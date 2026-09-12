# River and wildlife polish

Pinewatch now has five mallards swimming near Riverbend, two beavers on the west bank and two red foxes in the woods. Two Astra modelers created the original Blender meshes and textures. All nine animals remain present on Low and High graphics, with simpler distance meshes. Foxes and beavers pause when approached; ducks continue paddling.

The water mesh now ends at the actual terrain waterline instead of using a fixed width. Smaller irregular ripples replace the pronounced parallel bands. Stones and reed clumps follow the banks, and granite outcrops add detail to the mountain slopes. Walkable ground heights and ranch saves are unchanged.

## Running game

- [Mallard](benchmarks/wildlife-mallard.png)
- [Beaver](benchmarks/wildlife-beaver.png)
- [Red fox](benchmarks/wildlife-fox.png)
- [River](benchmarks/wildlife-river.png)
- [Mountain slopes](benchmarks/wildlife-overlook.png)

## Blender sources

- [Fox portrait](../ArtSource/Fox/fox-portrait.png) and [source notes](fox-art.md)
- [Mallard studio render](../ArtSource/RiverWildlife/duck-studio.png), [beaver studio render](../ArtSource/RiverWildlife/beaver-studio.png) and [source notes](river-wildlife-art.md)

These are ambient animals, like the existing deer and rabbits. Their movements are local scenery rather than synchronized server state. The meshes use simple whole-body movement; articulated walking, paddling and grooming animations remain future work. They do not attack, damage buildings or affect ranch products.

Tests and measurements are recorded in the [validation ledger](first-milestone.md). In addition to the model checks, habitat tests sample the shoreline along the river and ensure the duck routes stay over water and the beaver routes stay dry. The rendered tour captures each new species using a temporarily fixed pose for inspection.
