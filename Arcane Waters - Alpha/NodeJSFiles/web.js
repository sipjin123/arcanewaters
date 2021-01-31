// Web Navigation Requirements
const puppeteer = require('puppeteer');
const config = require ('./config.json');
const opn = require('opn');

// File Handling Requirements
const fs = require('fs');
const jsonfile = require('jsonfile')
var jsonFileHandler = require('./jsonFileHandler.js');
const cookiesFilePath = './data.json'
const versionsFilePath = './buildVersions.json';

// Event Requirements
var events = require('events');
var eventFinishedWritingData = new events.EventEmitter();
var eventFinishedReadingData = new events.EventEmitter();

var newCloudBuildData = [];
var latestBuildId = 0;
//========================================================
//Create an event handler:
var dataWriteEventHandler = function (result) {
  	console.log('Json Data writing is finished! Result: ' + result);
}
var dataReadEventHandler = function (result) {
  	console.log('Json Data reading is finished! Result: Fetched Entries: ' + result.length);
	try {
		var localJsonData = jsonFileHandler.readLocalData();
		var arrayOfObjects = localJsonData;
		console.log('Reading local JSON data success!');

		var newEntryCount = 0;
		console.log('-----------------------------------');
		for (var i = 0 ; i < result.length ; i ++){
			var filterData = localJsonData.filter(x => x.buildId == result[i].buildId);

			// If result is less than 1, it means data is not existing locally yet
			if (filterData.length < 1) {
				console.log("This is a new entry, registering locally: ID: " + result[i].buildId + " : " + filterData.length);
				
				var dataEntry = {
					buildId: result[i].buildId,
					message: result[i].message
				};
				var newArrayOfObjects = (localJsonData)
				newArrayOfObjects.push(dataEntry)
				newCloudBuildData.push(dataEntry)
				newEntryCount++;
			}
		}
		console.log('-----------------------------------');
		console.log("Finished updating local data, total added entries: " + newEntryCount);
		if (newCloudBuildData.length > 0) {
			postUpdates(newCloudBuildData, arrayOfObjects);
		} else {
			console.log('no new updates');
    		process.exit();
		}
	} catch {
		console.log('Failed to fetch json data');
		jsonFileHandler.writeJsonData(eventFinishedWritingData, result);
	}
}

// Assign the event handler to an event
eventFinishedWritingData.on('finishedWriting', dataWriteEventHandler);
eventFinishedReadingData.on('finishedReading', dataReadEventHandler);

// Begin to read the MySQL Data
jsonFileHandler.readJsonData(eventFinishedReadingData);

const postUpdates = async (newCloudObjects, arrayOfObjects) => {
	latestBuildId = newCloudObjects.length-1;
	const options = {
		path: 'images/Build_' + newCloudObjects[latestBuildId].buildId + '.png',
		fullPage: true,
		omitBackground: true,
		type: 'jpeg'
	}
	let steamUrl = 'https://steamcommunity.com/login/home/?goto=';
	let browser = await puppeteer.launch({ headless : true });
	let page = await browser.newPage();
	var actionInterval = 3000;
	var typingInterval = 1000;

	await page.goto(steamUrl, { waitUntil: 'networkidle2'});

    if(fs.existsSync(cookiesFilePath)) {
          console.log("The cookie file exists! Input Cookies");

		  const content = fs.readFileSync(cookiesFilePath);
		  const cookiesArr = JSON.parse(content);
		  if (cookiesArr.length !== 0) {
		    for (let cookie of cookiesArr) {
		      await page.setCookie(cookie)
		    }

		    // Login Credentials
			console.log("Entering Credentials");
	      	await page.type('#steamAccountName', config.userName, {delay:100});
			await page.type('#steamPassword', config.password, {delay: 100});
			await page.waitFor(actionInterval);
			await page.keyboard.press('Enter');
			console.log("Pressed Enter");
			await page.waitForNavigation()
			await page.waitFor(actionInterval);

			// Navigate to Steam Community
			console.log("Go to announcement page");
			let page2 = await browser.newPage();
			let link2Uril = 'https://steamcommunity.com/games/1266340/partnerevents/category/';
			await page2.goto(link2Uril, { waitUntil: 'networkidle2'});
		
			// Select Main Category
			console.log("Select announcement Button");
			await page2.waitFor(actionInterval);
			await page2.evaluate(() => {
			  var nameOfClass = 'partnereventeditor_EventCategory_Title_16pAy';
			  document.querySelector('.' + nameOfClass).click();
			});

			// Select Sub category
			console.log("Select patch Button");
			await page2.waitFor(actionInterval);
			await page2.evaluate(() => {
			  var nameOfClass = 'partnereventeditor_EventSubCategory_Desc_kjyqb';
			  document.querySelector('.' + nameOfClass).click();
			});

			// Register text field content
			await page2.waitFor(actionInterval);
			await page2.type('.partnereventeditor_EventEditorTitleInput_ZAOXn', 'Patch Notes!');

			await page2.waitFor(typingInterval);
			await page2.type('.partnereventeditor_Subtitle_32XZf', 'Build #' + newCloudObjects[latestBuildId].buildId);

			await page2.waitFor(typingInterval);
			await page2.type('.partnereventeditor_Summary_2eQap', 'Development Updates');

			await page2.waitFor(typingInterval);
			for (var messageIndex = 0 ; messageIndex < newCloudBuildData.length ; messageIndex ++) {
				const sentences = newCloudBuildData[messageIndex].message.split('\n');
				var sentenceList = [];

				for (var index = 0 ; index < sentences.length -1 ; index++) {
					var textLine = sentences[index];
					for (var charIndex = 0 ; charIndex < textLine.length ; charIndex++) {
						if (textLine[textLine.length-1] == ',') {
							console.log('Redundant comma! : ' +textLine);
							textLine = sentences[index].toString().substring(0, sentences[index].length -1);
						} else if (textLine[textLine.length-2] == ',') {
							console.log('Redundant comma! : ' +textLine);
							textLine = sentences[index].toString().substring(0, sentences[index].length -2);
						}
					}

				 	if (sentences[index].length > 1) {
						if (textLine[0] != '-') { 
					 		sentences[index] = '- ' + textLine;
					 		textLine = sentences[index];
					 	}
				 	}
				 	sentences[index] = '\n' + textLine;
				 	sentences[index][sentences.length-1] = '';
				 	sentenceList.push(sentences[index]);
				}
				await page2.type('.partnereventeditor_EventEditorDescription_3C8iP', sentenceList + '\n');
			}
			await page2.waitFor(actionInterval);

			// Take screen shot
			console.log("Taking screenshot");
    		await page2.screenshot(options);

			// Navigate to Publish Tab
			console.log("Select publish Button");
			await page2.waitFor(actionInterval);
			await page2.evaluate(() => {
					  var nameOfClass = 'tabbar_GraphicalAssetsTab_3lJb_ ';
					  document.querySelectorAll('.' + nameOfClass)[4].click();
					});
			await page2.waitFor(actionInterval);
			await page2.evaluate(() => {
			  var nameOfClass = 'partnereventshared_EventPublishButton_3nIAe';
			  document.querySelector('.' + nameOfClass).click();
			});

			// Trigger publish action
			await page2.waitFor(actionInterval);
			await page2.waitForSelector('button[type="submit"]');
			await page2.click('button[type="submit"]');
			await page2.waitFor(actionInterval);
			await page2.waitForSelector('button[type="submit"]');
			await page2.click('button[type="submit"]');

			// Finishing notification
			await page2.waitFor(actionInterval);
			jsonFileHandler.writeJsonData(eventFinishedWritingData, arrayOfObjects);
			await page2.waitFor(actionInterval);
		    console.log('Session has been loaded in the browser');
		} else {
		  console.log('Cookies length is: ' + cookiesArr.length);
		}
    } else {
        console.log('The file does not exist.');
        await page.type('#steamAccountName', config.userName, {delay:100});
		await page.type('#steamPassword', config.password, {delay: 100});
		await page.waitFor(2000);
		await page.keyboard.press('Enter');
		await page.waitForNavigation()
		
		await page.waitFor(8000);

		// Save Session Cookies
		const cookiesObject = await page.cookies();

		// Write cookies to temp file to be used in other profile pages
		jsonfile.writeFile(cookiesFilePath, cookiesObject, { spaces: 2 },
	 	function(err) { 
		  	if (err) {
		  		console.error(err)
		  	} else {
				console.log('write complete');
		  	}
		})
    }
	
	let data = await page.evaluate(() => {
	 let testtitle = document.querySelectorAll('div.titleBar');
		return {
			testtitle,
		}	
	})

	console.log(data);

    await browser.close();
    process.exit();
};
console.log('Initializing');