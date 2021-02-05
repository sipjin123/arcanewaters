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

class patchNoteClass {
    constructor(buildId, buildComment) {
        this.buildId = buildId;
        this.buildComment = buildComment;
    }

    buildId = 0;
    buildComment = "";
}

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

    connection.query('select dhBuildVersion, dhChangesetId, dhStatusReason from deploy_history where dhBuildTarget = "arcanewaters--ArcaneWaters-Server-Dev-Windows" and dhStatus = "COMPLETED" and dhBuildVersion > ' + buildValue, function(err,result){
        if (err) {
            console.log('Failed to fetch Jenkins Sql Data Complete');
            console.error(err);
            eventToEmit.emit('finishedCheckingJenkins', result);
        } else {
            console.log('Read Jenkins Sql Data Complete');
            getCommentsFromPlastic(eventToEmit, result);
        }
    });

}

getCommentsFromPlastic = function(eventToEmit, cachedContent) {
    var newClassList = [];
    var hasCompletedQuery = false;
    var queryResponseCounte = 0;
    var buildIdArray = [];

    // Log cache to make sure the process is working properly
    for (var i = 0 ; i < cachedContent.length ; i++) {
        console.log("Parameter cache is: "+cachedContent[i].dhBuildVersion);
        buildIdArray.push(cachedContent[i].dhBuildVersion);
    }
    for (var i = 0 ; i < buildIdArray.length ; i++) {
        console.log("BuildId cache is: "+buildIdArray[i]);
    }

    if (cachedContent.length < 1) {
        eventToEmit.emit('finishedCheckingJenkins', newClassList);
    } else  {
        for (var i = 0 ; i < cachedContent.length ; i++) {
            console.log("Fetching from database: "+cachedContent[i].dhBuildVersion + " "+cachedContent[i].dhChangesetId)
            var queryString = 'SELECT * FROM global.plastic_changeset where id = ' + cachedContent[i].dhChangesetId;
            connection.query(queryString, function(err,result){
                if (err) {
                    console.log('Failed to fetch plasitc changeset');
                    console.error(err);
                } else {
                    console.log('No error in fetching plasitc changeset: '+result.length+" Index: "+i);
                    for (var q = 0 ; q < result.length ; q++) {
                        var newClass = new patchNoteClass (
                            buildIdArray[i],
                            result[q].comment
                        );
                        
                        newClassList.push(newClass);
                    }
                    queryResponseCounte++;

                    if (queryResponseCounte >= cachedContent.length && !hasCompletedQuery) {
                        hasCompletedQuery = true;
                        console.log('-------------- Finished fetching from database '+newClassList.length+' --------------');

                        for (var r = 0 ; r < newClassList.length ; r++) {
                            newClassList[r].buildId = buildIdArray[r];
                            console.log('-> CompletedQuery: '+newClassList[r].buildId+ " : " +newClassList[r].buildComment);

                        }
                        eventToEmit.emit('finishedCheckingJenkins', newClassList);
                    }
                }
            });
        }
    }
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