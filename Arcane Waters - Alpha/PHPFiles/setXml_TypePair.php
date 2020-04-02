<?php
	$tableName = $_GET['tableName'];
	$textFileName = $_GET['textFileName'];
	
	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
		
	$query = "SELECT xml_id, xmlContent FROM ".$tableName;
	$myfile = fopen("xml_files/".$textFileName.".txt", "w") or die("Unable to open file!");
	
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
	echo "Finished loading: ".$tableName."\n";
	fclose($myfile);
		
	$mysqli -> close();
?>