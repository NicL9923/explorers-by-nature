# Original model art pass

Astra modelers produced original Blender assets for the ranch, woodland and wildlife. Each model has cheaper distance meshes. Unity preserves their individual material slots, coat/wood textures, and leaf transparency. Construction and animal interaction still use simple stable colliders independently of the visible detail.

## Running game

- [Homestead](benchmarks/art-ranch.png)
- [Clover](benchmarks/art-clover.png)
- [Hens and coop](benchmarks/art-hens.png)
- [Planted daisies](benchmarks/art-flowers.png)
- [Woodland wildlife](benchmarks/art-woodland.png)

## Blender previews

These are studio renders of the actual editable meshes, not concept paintings. Final game lighting differs.

- [Cow and hen contact sheet](../ArtSource/animal-contact-sheet.jpg)
- [Timber homestead and coop](../ArtSource/Homestead/homestead-assembled.png)
- [Pine and aspen](../ArtSource/Woodland/woodland-contact-sheet.png)
- [Tree distance meshes](../ArtSource/Woodland/woodland-lod-sheet.png)
- [Daisies](../ArtSource/Woodland/wildflower-detail.png)
- [Deer](../ArtSource/Wildlife/deer-portrait.png)
- [Cottontail](../ArtSource/Wildlife/rabbit-portrait.png)

## Source and budgets

The generators, editable Blender files, original textures and import details are recorded in [animal art](animal-art.md), [woodland art](woodland-art.md), [homestead art](homestead-art.md), and [wildlife art](wildlife-art.md). No third-party model or texture was added in this art pass.

Cow and hen near meshes stay below 24,000 and 12,000 triangles. The near trees are below 6,000 triangles each, with pine/aspen distant meshes below 700. The detailed shingle roof is approximately 9,100 triangles and its distant version approximately 1,100. The Unity import test checks triangle reductions, material assignments, foliage clipping and metre scale.

This is a coherent cozy art pass, not photogrammetry. Animals use static meshes under the prototype movement system; skeletal locomotion and facial animation remain future work. First-person explorer avatars and distant swallows remain simpler models.

Gameplay validation and measured player performance are recorded in [the validation ledger](first-milestone.md). Previous release benchmarks do not establish performance for these more detailed assets.
