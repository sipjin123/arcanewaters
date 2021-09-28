# Hat ClipMask Generation Tool
## Ken

### USAGE
1. Create a HatClipMaskGenerationSettings asset anywhere in the project with the Menu Item (Assets > Create > Hat Clip Mask Generation Tool - Settings)
2. Define the textures that will be processed in the HatClipMaskGenerationSettings asset that was created. Each texture should contain the frames of a hat.
	The asset allows to specify an extra texture for additional clipping. This texture can be used to tweak the final clip mask. This texture will be processed by the tool, by clipping out all the sprites defined in the texture.
3. Activate the tool with the Menu Item (Util > Generate Clip Masks for Hats)

### FUNCTIONALITY
The tool will delete previously generated clip masks (if there is any) and then proceed to generate a clip mask in the same folder of each of the processed textures.

	