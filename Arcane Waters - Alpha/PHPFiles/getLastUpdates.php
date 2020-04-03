<?php
	function fetchData($tableName, $mysqli) {
		$query = "SELECT lastUserUpdate FROM ".$tableName." order by lastUserUpdate DESC limit 1";
		if ($stmt = $mysqli->prepare($query)) {
			$stmt->execute();
			$stmt->bind_result($lastUserUpdate);

			while ($stmt->fetch()) {
				echo $tableName."[space]".$lastUserUpdate."[next]\n";
			}
			$stmt->close();
		} else {
			printf ("Failed to Query");
		}
	}

	/* make sure that the folder exists */
	if(!is_dir("xml_files")) {
		$path = 'xml_files';
		mkdir($path, null, true);
	} 

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
		
	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	fetchData("ability_xml_v2", $mysqli);
	fetchData("achievement_xml_v2", $mysqli);
	fetchData("background_xml_v2", $mysqli);
	fetchData("crafting_xml_v2", $mysqli);
	fetchData("crops_xml_v1", $mysqli);
	
	fetchData("equipment_armor_xml_v3", $mysqli);
	fetchData("equipment_helm_xml_v2", $mysqli);
	fetchData("equipment_weapon_xml_v3", $mysqli);
	fetchData("land_monster_xml_v3", $mysqli);
	fetchData("npc_xml", $mysqli);
	
	fetchData("player_class_xml", $mysqli);
	fetchData("player_faction_xml", $mysqli);
	fetchData("player_job_xml", $mysqli);
	fetchData("player_specialty_xml", $mysqli);
	fetchData("sea_monster_xml_v2", $mysqli);
	
	fetchData("ship_ability_xml_v2", $mysqli);
	fetchData("ship_xml_v2", $mysqli);
	fetchData("shop_xml_v2", $mysqli);
	fetchData("tutorial_xml", $mysqli);
	fetchData("usable_item_xml", $mysqli);
		
	$mysqli -> close();
?>
