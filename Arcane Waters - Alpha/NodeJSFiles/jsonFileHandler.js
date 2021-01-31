// Json Requirements
var jsonFileHandler = {};
const fs = require('fs');
const jsonfile = require('jsonfile')
const versionsFilePath = './buildVersions.json';
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

getLatestSteamBuild = function(eventToEmit) {
    connection.query('select buildId from steam_patch_status', function(err,result){
        if (err) {
            console.error(err);
            eventToEmit.emit('finishedCheckingSteamBuild', result);
        } else {
            console.log('Read Steam Build Complete');
            eventToEmit.emit('finishedCheckingSteamBuild', result);
        }
    });
}

updateLatestSteamBuild = function(eventToEmit, buildValue) {
    connection.query('UPDATE steam_patch_status SET buildId = '+buildValue, function(err,result){
        if (err) {
            console.error(err);
            eventToEmit.emit('finishedUpdatingSteamBuild', result);
        } else {
            console.log('Write Steam Build Complete');
            eventToEmit.emit('finishedUpdatingSteamBuild', result);
        }
    });
}

getUpdateLogsFromJenkins = function(eventToEmit, buildValue) {
    console.log('Jenkins Build Value is: '+ buildValue);

    connection.query('select buildId, message from cloud_changesets where buildId > ' + buildValue, function(err,result){
        if (err) {
            console.error(err);
            eventToEmit.emit('finishedCheckingJenkins', result);
        } else {
            console.log('Read Jenkins Sql Data Complete');
            eventToEmit.emit('finishedCheckingJenkins', result);
        }
    });
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
    getLatestSteamBuild,
    getUpdateLogsFromJenkins,
    updateLatestSteamBuild
};