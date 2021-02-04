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

getLatestSteamBuild = function(eventToEmit, buildType) {
    connection.query('select buildId from steam_patch_status where steamAppId="'+buildType+'"', function(err,result){
        if (err) {
            console.error(err);
            eventToEmit.emit('finishedCheckingSteamBuild', result);
        } else {
            console.log('Read Steam Build Complete');
            eventToEmit.emit('finishedCheckingSteamBuild', result);
        }
    });
}

updateLatestSteamBuild = function(eventToEmit, buildValue, buildType) {
    connection.query('UPDATE steam_patch_status SET buildId = '+buildValue+ ' where steamAppId="'+buildType+'"', function(err,result){
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

    connection.query('select dhBuildVersion, dhStatusReason from deploy_history where dhBuildTarget = "arcanewaters-WindowsServer" and dhBuildVersion > ' + buildValue, function(err,result){
        if (err) {
            console.error(err);
            getCommentsFromPlastic('finishedCheckingJenkins', result);
        } else {
            console.log('Read Jenkins Sql Data Complete');
            eventToEmit.emit('finishedCheckingJenkins', result);
        }
    });
}

getCommentsFromPlastic = function(eventToEmit, plasticId) {
    console.log('Jenkins Build Value is: '+ buildValue);

    connection.query('select dhBuildVersion, message from deploy_history where dhBuildTarget = "arcanewaters-WindowsServer" and dhBuildVersion > ' + buildValue, function(err,result){
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