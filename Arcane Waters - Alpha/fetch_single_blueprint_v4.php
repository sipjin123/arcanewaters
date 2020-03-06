<?php
	$bpId = $_GET['bpId'];
	$usrId = $_GET['usrId'];

	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
	
	$query = "SELECT itmId, itmCategory, itmType, arcane.crafting_xml_v2.xmlContent as craftingXML, 
		CASE 
			WHEN itmCategory = 7 and itmData like '%blueprintType=weapon%' THEN arcane.equipment_weapon_xml_v3.xmlContent
			WHEN itmCategory = 7 and itmData like '%blueprintType=armor%' THEN arcane.equipment_armor_xml_v3.xmlContent
		END AS equipmentXML
		FROM arcane.items 
		left join arcane.crafting_xml_v2 
		on (itmData like '%blueprintType=weapon%' and itmType = equipmentTypeID and arcane.crafting_xml_v2.equipmentCategory = 1) 
		or (itmData like '%blueprintType=armor%' and itmType = equipmentTypeID and arcane.crafting_xml_v2.equipmentCategory = 2)
		left join arcane.equipment_weapon_xml_v3 on (itmData like '%blueprintType=weapon%' and itmType = arcane.equipment_weapon_xml_v3.equipmentTypeID)
		left join arcane.equipment_armor_xml_v3 on (itmData like '%blueprintType=armor%' and itmType = arcane.equipment_armor_xml_v3.equipmentTypeID)
		where (itmCategory = 7 and itmId = ?) and items.usrId = ?";
	
	if ($stmt = $mysqli->prepare($query)) {
		$stmt->bind_param("ss", $bpId, $usrId);
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