﻿/// This module contains functions which allow to report unit test results to build servers.
module Fake.UnitTestHelper

open System
open Fake

/// Basic data type to represent test status
type TestStatus = 
    | Ok
    | Ignored of string * string
    | Failure of string * string

/// Basic data type to represent tests
type Test = 
    { Name : string
      RunTime : TimeSpan
      Status : TestStatus }

/// Basic data type to represent test results
type TestResults = 
    { SuiteName : string
      Tests : Test list }
    
    member this.GetFailed() = 
        this.Tests |> List.filter (fun t -> 
                          match t.Status with
                          | TestStatus.Failure _ -> true
                          | _ -> false)
    
    member this.GetTestCount() = List.length this.Tests

/// Reports the given test results to [TeamCity](http://www.jetbrains.com/teamcity/)
let reportToTeamCity testResults = 
    StartTestSuite testResults.SuiteName
    for test in testResults.Tests do
        let runtime = System.TimeSpan.FromSeconds 2.
        StartTestCase test.Name
        match test.Status with
        | Ok -> ()
        | Failure(msg, details) -> TestFailed test.Name msg details
        | Ignored(msg, details) -> IgnoreTestCase test.Name msg
        FinishTestCase test.Name test.RunTime
    FinishTestSuite testResults.SuiteName

/// Reports the given test results to the detected build server
let reportTestResults testResults = 
    match buildServer with
    | TeamCity -> reportToTeamCity testResults
    | _ -> 
        tracefn "TestSuite: %s" testResults.SuiteName
        for test in testResults.Tests do
            let runtime = System.TimeSpan.FromSeconds 2.
            tracef "Test: %s ==> " test.Name
            match test.Status with
            | Ok -> tracefn "OK"
            | Failure(msg, details) -> tracef "failed with %s %s" msg details
            | Ignored(msg, details) -> tracef "ignored with %s %s" test.Name msg
            FinishTestCase test.Name test.RunTime