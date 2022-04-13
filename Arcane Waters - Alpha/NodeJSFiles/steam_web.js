// Web Navigation Requirements
const puppeteer = require('puppeteer');
const config = require ('./config.json');
const opn = require('opn');
const ks = require('node-key-sender');

// File Handling Requirements
const fs = require('fs');
const jsonfile = require('jsonfile')
var jsonFileHandler = require('./jsonFileHandler.js');
const cookiesFilePath = './data.json'
const versionsFilePath = './buildVersions.json';

//========================================================
const postUpdates = async () => {
	const options = {
		path: 'images/Build_1.png',
		fullPage: true,
		omitBackground: true,
		type: 'jpeg'
	}

	console.log('trigger steam');
	let steamUrl = 'https://partner.steamgames.com/';
	let browser = await puppeteer.launch({
		executablePath: config.executablePath.toString(), 
		headless : false,
		userDataDir: config.browserProfile.toString()
	});
	console.log('new page');
	let page = await browser.newPage();
	await page.setViewport({width:0, height:0});

	var actionInterval = 2000;
	var typingInterval = 1000;

	console.log('entering steam');
	await page.goto(steamUrl, { waitUntil: 'networkidle2'}); 

    // Login Credentials
   
	console.log("Entering Credentials");
	try {
	    await page.type('#steamAccountName', config.userName, {delay:100});
		await page.type('#steamPassword', config.password, {delay: 100});
		await page.waitFor(actionInterval);
		await page.keyboard.press('Enter');
		console.log("Pressed Enter");
		await page.waitForNavigation()
	} catch{
		console.log("No input fields");
	}

	await page.waitFor(actionInterval);

	// Navigate to Steam Community
	console.log("Go to announcement page");
	let link2Uril = 'https://partner.steamgames.com/apps/builds/1266340';
	await page.goto(link2Uril, { waitUntil: 'networkidle2'});

	await page.waitFor(actionInterval);

	let data1 = await page.evaluate(() => {
  		const rows = document.querySelectorAll('tr');
		  return Array.from(rows, row => {
	    const columns = row.querySelectorAll('td');
	    return Array.from(columns, column => column.innerText);
	  });	
	})

	// Extract fetched table array
	var indexCategory = 9;//6;
	var fetchedData = data1[indexCategory];
	var currentBranch = fetchedData[0];
	var buildId = fetchedData[1];
	var preString = 'select[id="betakey_';
	var postString = '"]';
	var dropDownKey = preString + buildId + postString;
	console.log(dropDownKey);

	// Enable for Logging
	var scapeLog = false;
	if (scapeLog) {
		for (var i = 6 ; i < 30; i++ ) {
			var testLog = data1[i];
				console.log(i); // Shows Index
				console.log(testLog[1]); // Shows First Element
				console.log(testLog[2]); // Shows Second Element
				console.log("--------------------------------");
		}
	}

	if (currentBranch.includes('production')) {
		// Select dropdown key
	    console.log('Click on Dropdown');
		await page.select(dropDownKey, 'default');
		const selectElem = await page.$(dropDownKey);
		await selectElem.type('default');
	  	console.log(fetchedData);

		// Simulate submit and next page
	    console.log('Click on Submit');
		await page.keyboard.press('Tab');
		await page.waitFor(actionInterval);
		await page.keyboard.press('Enter');
		await page.waitFor(actionInterval);

		// Select the publish
		try {
		    console.log('Finalize Page');
			await page.waitFor(actionInterval);
			page.on('dialog', async dialog => {
			    console.log(dialog.message());
				await page.waitFor(actionInterval);
			    await dialog.accept();
			});
		    await page.click('button[type=button]');
		} catch { 
			await page.waitFor(actionInterval);
	   		console.log('Failed to process page');
		}
	} else {
   		console.log('This is not a production branch!');
   		console.log(currentBranch);
	}
	await page.waitFor(5000);
    await browser.close();
    process.exit();
};

console.log('Initializing');	
postUpdates();