const puppeteer = require('puppeteer');
const config = require ('./config.json');
const Nylas = require('nylas');
const opn = require('opn');

const fs = require('fs');
const jsonfile = require('jsonfile')
const cookiesFilePath = 'E:/Desktop/WebBotSteamCommunity/data.json'

var express = require('express');
var mysql = require('mysql');
var connection = mysql.createConnection({
	// Properties
	host: 'dev.c1whxibm6zeb.us-east-2.rds.amazonaws.com',
	user: 'userAdKmE',
	password: 'HEqbVDsvvCza5n4N',
	database: 'arcane'
});
var app = express();

/*
private static string _remoteServer = "dev.c1whxibm6zeb.us-east-2.rds.amazonaws.com"; 
   private static string _database = "arcane";
   private static string _uid = "userAdKmE";
   private static string _password = "HEqbVDsvvCza5n4N";
*/

connection.connect(function(error){
	if(!!error){
		console.log('Error');
	} else	{
		console.log('Connected');
	}
});

Nylas.config({
    clientId: 'b0ntakun.games@gmail.com',
    clientSecret: 'B0ntakun2827',
});

const postUpdates = async () => {
	const options = {
		path: 'images/website.png',
		fullPage: true,
		omitBackground: true,
		type: 'jpeg'
	}
	let steamUrl = 'https://steamcommunity.com/login/home/?goto=';
	let browser = await puppeteer.launch({ headless : false });
	let page = await browser.newPage();
	var actionInterval = 3000;

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
			await page2.type('.partnereventeditor_EventEditorTitleInput_ZAOXn', 'Title Test');

			await page2.waitFor(actionInterval);
			await page2.type('.partnereventeditor_Subtitle_32XZf', 'Subtitle Test');

			await page2.waitFor(actionInterval);
			await page2.type('.partnereventeditor_Summary_2eQap', 'Summary Test');

			await page2.waitFor(actionInterval);
			await page2.type('.partnereventeditor_EventEditorDescription_3C8iP', 'Description Test');

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
		    console.log('Done Typing');
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
};

//console.log('Initializing');
//postUpdates();