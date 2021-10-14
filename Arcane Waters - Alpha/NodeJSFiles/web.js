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
var logTextFile = "";
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
	console.log('Jenkins build complete: Results are: '+ result);
	try {
		console.log('Patchnote text files are: ('+ patchNoteMessage.length + ") From: " + logTextFile);
		if (patchNoteMessage.length > 1) {
			postUpdates(result);
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

if (process.argv[2] && process.argv[2] === '-prod') {
  console.log('This is the Main Build!');
  buildType = "main";
} 

if (process.argv[2] && process.argv[2] === '-playtest') {
  console.log('This is a Playtest Build!');
  buildType = "playtest";
} 

console.log('Reading Patch Note Text File')

console.log('------------------------------ Read Start');

logTextFile = process.argv[3];
var patchNoteMessage = "";
if (process.argv[3]) {
    var fs2 = require('fs'), filename = process.argv[3];
    fs2.readFile(filename, 'utf8', function(err, data) {
      if (err) throw err;
      patchNoteMessage = data;
      console.log(patchNoteMessage);
    });
}

console.log('------------------------------ Read End');
//========================================================

// Assign the event handler to an event
eventFinishedReadingSteamBuildData.on('finishedCheckingSteamBuild', dataReadSteamBuildEventHandler);
eventFinishedReadingJenkinsData.on('finishedCheckingJenkins', dataReadJenkinsEventHandler);
eventFinishedUpdatingSteamData.on('finishedUpdatingSteamBuild', dataWriteSteamBuildEventHandler);

// Read the latest build id published on steam
jsonFileHandler.getLatestSteamBuild(eventFinishedReadingSteamBuildData, buildType);

//========================================================

// TODO: Use this function for selecting all previously uploaded images after learning how its possible to use parameters inside page.evaluate()
const selectImageSelector = async (page2) => {
	var actionInterval = 3000;

	// Navigate to Previously Uploaded Assets
	console.log("Navigate to Previously Uploaded Assets");

	await page2.waitFor(actionInterval);
	await page2.evaluate(() => {
			  var nameOfClass = 'clanimagepicker_SelectImageButton__R_zU ';
			  document.querySelectorAll('.' + nameOfClass)[1].click();
			});
	await page2.waitFor(actionInterval);

    // Find the images to attach which is from the previous graphic assets upload
	const rect = await page2.evaluate(() => {
	  	var nameOfClass = 'clanimagepicker_ImageWrapper_vYrtX';
	    const allQuery = document.querySelectorAll('.' + nameOfClass);
	    const element = allQuery[allQuery.length - 1];
	    if (!element) return null;
	    const { x, y } = element.getBoundingClientRect();
	    return { x, y };
	});

	// After finding the image to select, simulate double click
	if (rect) {
	    await page2.mouse.click(rect.x, rect.y, { clickCount: 2 });
    } else {
    	console.error("Element Not Found for index: " + 1);
    }

	await page2.waitFor(actionInterval);

	console.log("Done Uploaded Assets");
}

const postUpdates = async (buildIdDeployed) => {
	const options = {
		path: 'images/Build_' + buildIdDeployed + '.png',
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

			// Non playtest patch updater has different ui, no option for categorized patch note
			if (buildType != "playtest") {
				// Select Sub category
				console.log("Select patch Button");
				await page2.waitFor(actionInterval);
				await page2.evaluate(() => {
				  var nameOfClass = 'partnereventeditor_EventSubCategory_Desc_kjyqb';
				  document.querySelector('.' + nameOfClass).click();
				});
			}

			// Register text field content
			await page2.waitFor(actionInterval);
			var patchName = "";
			if (buildType == "playtest") {
				patchName = "PlayTest ";
			}
			await page2.type('.partnereventeditor_EventEditorTitleInput_ZAOXn', patchName + 'Patch Notes Build#' + buildIdDeployed);

			await page2.waitFor(typingInterval);
			await page2.type('.partnereventeditor_Subtitle_32XZf', 'Build #' + buildIdDeployed);

			await page2.waitFor(typingInterval);
			await page2.type('.partnereventeditor_Summary_2eQap', 'Production Updates');

			await page2.waitFor(typingInterval);
			
      	  await page2.type('.partnereventeditor_EventEditorDescription_3C8iP', patchNoteMessage + '\n');
			await page2.waitFor(actionInterval);

			// =======================================================================
			// Non playtest patch updater does not have option to select build attachment
			if (buildType != "playtest") {
				// Select the build target
				await page2.waitFor(actionInterval);
				const findBuildSelectButton = await page2.$$('button');
				if (findBuildSelectButton.length < 1) {
					console.log('Missing select build button');
				} else {
					findBuildSelectButton[0].click();
				}

				console.log('Select Build Attachment');

				var dropDownIndividualData = 'dropdown_DialogDropDownMenu_Item_2oAiZ';
				var indexMeter = 'data-dropdown-index';
				var dropDownData = 'dropdown_DialogDropDownMenu_30wJO _DialogInputContainer';
				var productionBranch = 'production - Build 7517457 (10/12/2021)';
				var dropdownClassName = 'DialogDropDown _DialogInputContainer  Panel Focusable';

				await page2.waitFor(actionInterval);

				if (buildType == "main") {
					await page2.evaluate(() => {
						document.querySelector("div.DialogDropDown_Arrow").click();
						Array.from(document.querySelectorAll("div[data-dropdown-index]").values())[1].click();
						document.querySelector("button.Primary").click();
					});
				} else if (buildType == "playtest") {
					await page2.evaluate(() => {
						document.querySelector("div.DialogDropDown_Arrow").click();
						Array.from(document.querySelectorAll("div[data-dropdown-index]").values())[3].click();
						document.querySelector("button.Primary").click();
					});
				} else {
						const [button] = await page2.$x("//button[contains(., 'Confirm')]");
						if (button) {
						    await button.click();
						}
				}
			}
			// =======================================================================
			// Navigate to Graphic Assets Tab
			console.log("Navigate to Graphic Assets Tab");
			await page2.waitFor(actionInterval);
			await page2.evaluate(() => {
					  var nameOfClass = 'tabbar_GraphicalAssetsTab_3lJb_ ';
					  document.querySelectorAll('.' + nameOfClass)[2].click();
					});
			await page2.waitFor(actionInterval);
			// ==============================================================================
			// Navigate to Previously Uploaded Assets
			console.log("Navigate to Previously Uploaded Assets 1");
			await page2.waitFor(actionInterval);
			await page2.evaluate(() => {
					  var nameOfClass = 'clanimagepicker_SelectImageButton__R_zU ';
					  document.querySelectorAll('.' + nameOfClass)[1].click();
					});
			await page2.waitFor(actionInterval);

			// Double Click graphic image
			console.log("Double Click graphic image 1");
			await page2.waitFor(actionInterval);

			// Find the images to attach which is from the previous graphic assets upload
	  		const rect1 = await page2.evaluate(() => {
				  	var nameOfClass = 'clanimagepicker_ImageWrapper_vYrtX';
				    const allQuery = document.querySelectorAll('.' + nameOfClass);
				    const element = allQuery[allQuery.length - 1];
				    if (!element) return null;
				    const { x, y } = element.getBoundingClientRect();
				    return { x, y };
				});

			// After finding the image to select, simulate double click
			if (rect1) {
			    await page2.mouse.click(rect1.x, rect1.y, { clickCount: 2 });
		    } else {
		    	console.error("Element Not Found 1");
		    }

			await page2.waitFor(actionInterval);
			// ==========================

			// Navigate to Previously Uploaded Assets
			console.log("Navigate to Previously Uploaded Assets 2");
			await page2.waitFor(actionInterval);
			await page2.evaluate(() => {
					  var nameOfClass = 'clanimagepicker_SelectImageButton__R_zU ';
					  document.querySelectorAll('.' + nameOfClass)[1].click();
					});
			await page2.waitFor(actionInterval);

			// Double Click graphic image
			console.log("Double Click graphic image 2");
			await page2.waitFor(actionInterval);

			// Find the images to attach which is from the previous graphic assets upload
	  		const rect2 = await page2.evaluate(() => {
				  	var nameOfClass = 'clanimagepicker_ImageWrapper_vYrtX';
				    const allQuery = document.querySelectorAll('.' + nameOfClass);
				    const element = allQuery[allQuery.length - 2];
				    if (!element) return null;
				    const { x, y } = element.getBoundingClientRect();
				    return { x, y };
				});

			// After finding the image to select, simulate double click
			if (rect1) {
			    await page2.mouse.click(rect2.x, rect2.y, { clickCount: 2 });
		    } else {
		    	console.error("Element Not Found 2");
		    }

			await page2.waitFor(actionInterval);
			// ==========================

			// Navigate to Previously Uploaded Assets
			console.log("Navigate to Previously Uploaded Assets 3");
			await page2.waitFor(actionInterval);
			await page2.evaluate(() => {
					  var nameOfClass = 'clanimagepicker_SelectImageButton__R_zU ';
					  document.querySelectorAll('.' + nameOfClass)[1].click();
					});
			await page2.waitFor(actionInterval);

			// Double Click graphic image
			console.log("Double Click graphic image 3");
			await page2.waitFor(actionInterval);

			// Find the images to attach which is from the previous graphic assets upload
	  		const rect3 = await page2.evaluate(() => {
				  	var nameOfClass = 'clanimagepicker_ImageWrapper_vYrtX';
				    const allQuery = document.querySelectorAll('.' + nameOfClass);
				    const element = allQuery[allQuery.length - 3];
				    if (!element) return null;
				    const { x, y } = element.getBoundingClientRect();
				    return { x, y };
				});

			// After finding the image to select, simulate double click
			if (rect1) {
			    await page2.mouse.click(rect3.x, rect3.y, { clickCount: 2 });
		    } else {
		    	console.error("Element Not Found 3");
		    }

			await page2.waitFor(actionInterval);
			// ==============================================================================
			var blockPublishCommand = false;
			if (!blockPublishCommand) {
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
				console.log("Select publish Button Phase 1");
				await page2.waitFor(actionInterval);
				await page2.waitFor(actionInterval);
				await page2.waitFor(actionInterval);

				await page2.evaluate(() => {
						  var nameOfClass = 'tabbar_GraphicalAssetsTab_3lJb_ ';
						  document.querySelectorAll('.' + nameOfClass)[4].click();
						});

				console.log("Select publish Button Phase 2");

				await page2.waitFor(actionInterval);
				await page2.evaluate(() => {
						  var nameOfClass = 'tabbar_GraphicalAssetsTab_3lJb_ ';
						  document.querySelectorAll('.' + nameOfClass)[0].click();
						});

				console.log("Select publish Button Phase 3");

				await page2.waitFor(actionInterval);
				await page2.evaluate(() => {
						  var nameOfClass = 'tabbar_GraphicalAssetsTab_3lJb_ ';
						  document.querySelectorAll('.' + nameOfClass)[4].click();
						});

				console.log("Select publish Button Phase 4");

				await page2.waitFor(actionInterval);
				await page2.waitFor(actionInterval);
				await page2.waitFor(actionInterval);
				await page2.evaluate(() => {
				  var nameOfClass = 'partnereventshared_EventPublishButton_3nIAe';
				  document.querySelector('.' + nameOfClass).click();
				});

				console.log("Select publish Button Phase 5");

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

				jsonFileHandler.updateLatestSteamBuild(eventFinishedUpdatingSteamData, buildIdDeployed + 1, buildType);

				// Clear the text file after publish
				fs.writeFile(logTextFile, "", err => {
					if (err) {
					   console.error(err)
					   return
					}
					console.log("Finished clearing content of text file: " + logTextFile);
				})
			}
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