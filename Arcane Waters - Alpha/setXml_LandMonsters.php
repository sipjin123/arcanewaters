<?php
	$version = $_GET['version'];
	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
		
	// Fetch all land monsters info and save to text file
	$query = "SELECT xmlContent,xml_id
		FROM land_monster_xml_v3";
	$myfile = fopen("xml_files/land_monsters.txt", "w") or die("Unable to open file!");
	
	if ($stmt = $mysqli->prepare($query)) {
	    $stmt->execute();
		$stmt->bind_result($xmlContent, $xml_id);

		while ($stmt->fetch()) {
			$txt = $xml_id."[space]".$xmlContent."[next]\n";
			fwrite($myfile, $txt);
		}
		$stmt->close();
	} else {
		printf ("Failed to Query");
	}
	fclose($myfile);
	$mysqli -> close();
	
	echo "Finished Setting LandMonsters Data";
?>