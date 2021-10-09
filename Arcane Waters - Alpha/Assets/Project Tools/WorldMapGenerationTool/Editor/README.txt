# World Map Generation Tool
## Ken

### USAGE
1. Create a WorldMapGenerationSettings asset anywhere in the project with the Menu Item (Assets > Create > World Map Generation Tool - Settings).
2. Specify the source texture that defines the world map layout, and specify the number of columns and rows that the world will be divided in.
3. Activate the tool with the Menu Item (Util > Generate World Map)

### FUNCTIONALITY
The tool will create data objects that represent each sector of the map and upload them to the database.

### NOTES
The tool will produce temporary in a new folder on the Desktop. These files can be safely deleted after the process is completed.
If "Should Clean Up" in the WorldMapGenerationSettings is checked, the tool will automatically delete these files.