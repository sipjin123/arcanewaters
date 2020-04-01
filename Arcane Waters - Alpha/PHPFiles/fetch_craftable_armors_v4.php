<?php
	$usrId = $_GET['usrId'];

	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
	
	$query = "SELECT itmId, itmCategory, itmType, crafting_xml_v2.xmlContent as craftingXML, equipment_armor_xml_v3.xmlContent as equipmentXML FROM items 
		right join crafting_xml_v2 on (itmType = crafting_xml_v2.equipmentTypeID and itmData like '%blueprintType=armor%' and crafting_xml_v2.equipmentCategory = 2) 
		right join equipment_armor_xml_v3 on (itmType = equipment_armor_xml_v3.equipmentTypeID and itmData like '%blueprintType=armor%' ) 
		where (itmCategory = 7) and items.usrId = ?";
	
	if ($stmt = $mysqli->prepare($query)) {
		$stmt->bind_param("s", $usrId);
	    $stmt->execute();
		$stmt->bind_result($itmId, $itmCategory, $itmType, $craftingXML, $equipmentXML);

		while ($stmt->fetch()) {
			printf ("[next]%s[space]%s[space]%s[space]%s[space]%s[space]", $itmId, $itmCategory, $itmType, $craftingXML, $equipmentXML);
		}
		$stmt->close();
	} else {
		printf ("Failed to Query");
	}
	
	$mysqli -> close();
?>