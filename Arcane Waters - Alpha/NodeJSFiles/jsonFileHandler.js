// Json Requirements
var jsonFileHandler = {};
const fs = require('fs');
const jsonfile = require('jsonfile')
const versionsFilePath = 'E:/Desktop/WebBotSteamCommunity/buildVersions.json';
const config = require ('./config.json');

// Database Requirements
var express = require('express');
var mysql = require('mysql');
var connection = mysql.createConnection({
    // Properties
    host: config.sqlHost,
    user: config.sqlUser,
    password: config.sqlPassword,
    database: config.sqlDatabase
});


var events = require('events');
var eventEmitter = new events.EventEmitter();
//========================================================

readLocalData = function (){
    let rawdata = fs.readFileSync(versionsFilePath);
    let jsonData = JSON.parse(rawdata)
    return jsonData;
};

readJsonData = function(eventToEmit) {
    connection.query('select buildId, message from cloud_changesets', function(err,result){
        if (err) {
            console.error(err);
            eventToEmit.emit('finishedReading', result);
        } else {
            console.log('Read Sql Data Complete');
            eventToEmit.emit('finishedReading', result);
        }
    });
}

writeJsonData = function(eventToEmit, result) {
    jsonfile.writeFile(versionsFilePath, result, { spaces: 2 }, function(err) { 
        if (err) {
            console.error(err);
            eventToEmit.emit('finishedWriting', 'Fail!');
        } else {
            eventToEmit.emit('finishedWriting', 'Success!');
        }
    })
}

connectionTest = function(eventToEmit) {
    connection.connect(function(error){
        if(!!error){
            console.log('Error');
        } else  {
            console.log('Connected');
        }
    });
}

module.exports = {
    connectionTest,
    writeJsonData,
    readLocalData,
    readJsonData
};