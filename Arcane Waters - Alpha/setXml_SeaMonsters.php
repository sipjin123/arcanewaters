<?php
	$version = $_GET['version'];
	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
	
	// Fetch all sea monsters info and save to text file
	$query = "SELECT xml_id, xmlContent
		FROM sea_monster_xml_v2";
	$myfile = fopen("xml_files/sea_monsters.txt", "w") or die("Unable to open file!");
	
	if ($stmt = $mysqli->prepare($query)) {
	    $stmt->execute();
		$stmt->bind_result($xml_id, $xmlContent);

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
	
	echo "Finished Setting SeaMonsters Data";
?>