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
var eventFinishedReadingSteamBuildData = new events.EventEmitter();
var eventFinishedReadingJenkinsData = new events.EventEmitter();
var eventFinishedUpdatingSteamData = new events.EventEmitter();

var newCloudBuildData = [];
var latestSteamBuild = 0;
var buildType = "main";
//========================================================
//Create event handlers:

// This function fetches the steam build id of the latest build uploaded in the patch notes 
var dataReadSteamBuildEventHandler = function (result) {
	console.log('Steam build complete: '+result.length);
	try {
		for (var i = 0 ; i < result.length ; i ++){
			latestSteamBuild =  result[i].buildId;
			console.log('write complete: '+ latestSteamBuild);
		}
		jsonFileHandler.getUpdateLogsFromJenkins(eventFinishedReadingJenkinsData, latestSteamBuild);
	} catch {
		console.log('Failed to fetch json data');
	}
}	

// This function extracts all the changelogs from the database which was from jenkins changelogs
var dataReadJenkinsEventHandler = function (result) {
	console.log('Jenkins build complete: '+result.length);
	try {
		for (var i = 0 ; i < result.length ; i ++){
			console.log('write complete: '+ result[i].buildId+ " : " +result[i].buildComment);

			var dataEntry = {
				buildId: result[i].buildId,
				message: result[i].buildComment
			};
			newCloudBuildData.push(dataEntry);
		}

		if (newCloudBuildData.length > 0) {
			postUpdates(newCloudBuildData);
		} else {
			console.log('no new updates');
    		process.exit();
		}
	} catch {
		console.log('Failed to fetch json data');
	}
}	

// This function updates the database table build number for future reference
var dataWriteSteamBuildEventHandler = function (result) {
	console.log('Steam build number was successfully modified');
}	

//========================================================

if (process.argv.length === 2) {
  console.error('Expected at least one argument!');
  process.exit(1);
}

if (process.argv[2] && process.argv[2] === '-a') {
  console.log('This is the Main Build!');
  buildType = "main";
} 

if (process.argv[2] && process.argv[2] === '-b') {
  console.log('This is a Playtest Build!');
  buildType = "playtest";
} 

//========================================================

// Assign the event handler to an event
eventFinishedReadingSteamBuildData.on('finishedCheckingSteamBuild', dataReadSteamBuildEventHandler);
eventFinishedReadingJenkinsData.on('finishedCheckingJenkins', dataReadJenkinsEventHandler);
eventFinishedUpdatingSteamData.on('finishedUpdatingSteamBuild', dataWriteSteamBuildEventHandler);

// Read the latest build id published on steam
jsonFileHandler.getLatestSteamBuild(eventFinishedReadingSteamBuildData, buildType);

//========================================================

const postUpdates = async (newCloudObjects) => {
	const options = {
		path: 'images/Build_' + newCloudObjects[newCloudObjects.length-1].buildId + '.png',
		fullPage: true,
		omitBackground: true,
		type: 'jpeg'
	}
	let steamUrl = 'https://steamcommunity.com/login/home/?goto=';
	let browser = await puppeteer.launch({ headless : false });
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
	      	await page.type('#input_username', config.userName, {delay: 100});//#steamAccountName
			await page.type('#input_password', config.password, {delay: 100});//steamPassword
			await page.waitFor(actionInterval);
			await page.keyboard.press('Enter');
			console.log("Pressed Enter");
			await page.waitForNavigation()
			await page.waitFor(actionInterval);

			// Navigate to Steam Community
			console.log("Go to announcement page");
			let page2 = await browser.newPage();
			let link2Uril = '';
			if (buildType == "main") {
				link2Uril = 'https://steamcommunity.com/games/1266340/partnerevents/category/';
			} else {
				link2Uril = 'https://steamcommunity.com/games/1489170/partnerevents/category/';
			}
			
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
			var patchName = "";
			if (buildType == "playtest") {
				patchName = "PlayTest ";
			}
			await page2.type('.partnereventeditor_EventEditorTitleInput_ZAOXn', patchName + 'Patch Notes Build#' + newCloudObjects[newCloudObjects.length-1].buildId);

			await page2.waitFor(typingInterval);
			await page2.type('.partnereventeditor_Subtitle_32XZf', 'Build #' + newCloudObjects[newCloudObjects.length-1].buildId);

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
			//console.log("Taking screenshot");
    		//await page2.screenshot(options);

			// Navigate to Graphic Assets Tab
			console.log("Navigate to Graphic Assets Tab");
			await page2.waitFor(actionInterval);
			await page2.evaluate(() => {
					  var nameOfClass = 'tabbar_GraphicalAssetsTab_3lJb_ ';
					  document.querySelectorAll('.' + nameOfClass)[2].click();
					});
			await page2.waitFor(actionInterval);


			// Navigate to Previously Uploaded Assets
			console.log("Navigate to Previously Uploaded Assets");
			await page2.waitFor(actionInterval);
			await page2.evaluate(() => {
					  var nameOfClass = 'clanimagepicker_SelectImageButton__R_zU ';
					  document.querySelectorAll('.' + nameOfClass)[1].click();
					});
			await page2.waitFor(actionInterval);

			// Double Click graphic image
			console.log("Double Click graphic image");
			await page2.waitFor(actionInterval);

			// Find the image to attach which is from the previous graphic asset upload
	  		const rect = await page2.evaluate(() => {
				  	var nameOfClass = 'clanimagepicker_ImageWrapper_vYrtX';
				    const element = document.querySelectorAll('.' + nameOfClass)[0];
				    if (!element) return null;
				    const { x, y } = element.getBoundingClientRect();
				    return { x, y };
				});

			// After finding the image to select, simulate double click
			if (rect) {
			    await page2.mouse.click(rect.x, rect.y, { clickCount: 2 });
		    } else {
		    	console.error("Element Not Found");
		    }

			await page2.waitFor(actionInterval);

			// Naviate through all buttons in the page and select the upload button
			console.log("Select Upload Image Button");
			const elHandleArray = await page2.$$('button')
			// Total of 10 buttons will be pressed (Date Updated: 04-29-2021) This might change if steam updates their page
			// 0 upload
			// 1 cancel
			// 2 cover example
			// 3-10 Etc...
			elHandleArray[0].click();
			
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
			await page2.waitFor(actionInterval);
		    console.log('Session has been loaded in the browser');

			//jsonFileHandler.updateLatestSteamBuild(eventFinishedUpdatingSteamData, newCloudObjects[newCloudObjects.length-1].buildId, buildType);
		} else {
		  console.log('Cookies length is: ' + cookiesArr.length);
		}
    } else {
        console.log('The file does not exist.');
        await page.type('#input_username', config.userName, {delay: 100});//steamAccountName
		await page.type('#input_password', config.password, {delay: 100});//steamPassword
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