<?php
	$tableName = $_GET['tableName'];
	$textFileName = $_GET['textFileName'];

	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
		
	$dir = "xml_files/";
	$file = $dir.$textFileName.".txt";
	$current = "";
		
	$query = "SELECT xml_id, xmlContent FROM ".$tableName;
    /* $myfile = fopen(, "w+") or die("Unable to open file!"); */
	
	if ($stmt = $mysqli->prepare($query)) {
		$stmt->execute();
		$stmt->bind_result($xml_id, $xmlContent);

		while ($stmt->fetch()) {
			$txt = $xml_id."[space]".$xmlContent."[next]\n";
			$current .= $txt;
			/* fwrite($myfile, $txt); */
		}
		$stmt->close();
	} else {
		printf ("Failed to Query");
	}
	if(!file_put_contents ($file, $current)) {
		echo "Unable to open file!";
		if (!is_dir($dir) or !is_writable($dir)) {
			echo "Error: The directory doesn't exist or isn't writable.";
		} elseif (is_file($file) and !is_writable($file)) {
			echo "Error: The file exists and isn't writable.";
		}
	}
	/* fclose($myfile); */
	$mysqli -> close();
	
	echo "Finished loading: ".$tableName."\n";
?>