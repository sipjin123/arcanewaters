// Web Navigation Requirements
const puppeteer = require('puppeteer');
const config = require('./config.json');
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
    console.log('trigger steam');
    let steamUrl = 'https://partner.steamgames.com/';
    let browser = await puppeteer.launch({
        executablePath: config.executablePath.toString(),
        headless: false,
        userDataDir: config.browserProfile.toString()
    });
    console.log('new page');
    let page = await browser.newPage();
    await page.setViewport({
        width: 0,
        height: 0
    });

    var actionInterval = 2000;
    var typingInterval = 1000;

    console.log('entering steam');
    await page.goto(steamUrl, {
        waitUntil: 'networkidle2'
    });

    // Login Credentials

    console.log("Entering Credentials");
    try {
        await page.type('#steamAccountName', config.userName, {delay:100});
        await page.type('#steamPassword', config.password, {delay: 100});

        await page.waitFor(actionInterval);
        await page.keyboard.press('Enter');
        console.log("Pressed Enter");
        await page.waitForNavigation()
        await page.waitFor(actionInterval);
    } catch {
        console.log("Skip Credentials");
    }


    // Navigate to Steam Community
    console.log("Go to depot page");
    let link2Uril = 'https://partner.steamgames.com/apps/builds/1266340';
    await page.goto(link2Uril, {
        waitUntil: 'networkidle2'
    });

    await page.waitFor(actionInterval);

    let data1 = await page.evaluate(() => {
        const rows = document.querySelectorAll('tr');
        return Array.from(rows, row => {
            const columns = row.querySelectorAll('td');
            return Array.from(columns, column => column.innerText);
        });
    })

    // Extract fetched table array
    var indexCategory = 9;
    var fetchedData = data1[indexCategory];
    var currentBranch = fetchedData[0];
    var buildId = fetchedData[1];
    var preString = 'select[id="betakey_';
    var postString = '"]';
    var dropDownKey = preString + buildId + postString;
    console.log(dropDownKey);
    console.log(fetchedData);

    // Enable for Logging
    var scapeLog = false;
    if (scapeLog) {
        for (var i = 6; i < 30; i++) {
            var testLog = data1[i];
            console.log(i); // Shows Index
            console.log(testLog[0]);
            console.log(testLog[1]); // Shows First Element
            console.log(testLog[2]); // Shows Second Element
            console.log(testLog[3]);
            console.log("--------------------------------");
        }
    }

    if (currentBranch.includes('production')) {
        console.log('This is a production branch!');
        try {
            const data = fs.readFileSync('./build_cache.txt', 'utf8')
            var splitStri = data.split(":");
            console.log(splitStri);
            if (splitStri.length > 1) {
                var lastBuildType = splitStri[0];
                var lastBuildId = splitStri[1];

                console.log('LastBldType:' + lastBuildType);
                console.log('LastBldId:' + lastBuildId);
                if (buildId > lastBuildId) {
                    console.log('This is a new build id:{' + buildId + '} for production branch, last build was:{' + lastBuildId + '}!');
                    var newFileContent = currentBranch + ':' + buildId;
                    fs.writeFile('./build_cache.txt', newFileContent, function(err) {
                        if (err) return console.log(err);
                        console.log('Write Success! {' + newFileContent + '}');

                        // YEV: Setup Return Result Value here: TRUE
                    });
                } else {
                    console.log('There is no new build id:{' + buildId + '} for production branch!');
                    
                    // YEV: Setup Return Result Value here: FALSE
                }
            } else {
                console.error("Invalid String value from loaded text file");
                // YEV: Setup Return Result Value here: FALSE
            }
        } catch (err) {
            console.error(err);
            // YEV: Setup Return Result Value here: FALSE
        }

    } else {
        console.log('This is not a production branch! {' + currentBranch + '}:{' + buildId + '}');
        // YEV: Setup Return Result Value here: FALSE
    }
    await page.waitFor(5000);
    await browser.close();
    process.exit();
};

console.log('Initializing');
postUpdates();