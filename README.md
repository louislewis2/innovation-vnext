# Innovation.vNext

## CQRS

A simple framework which aims to provide the ability to use a CQRS pattern in your code base,
currently with immediate consistency. It does not implement or try support Event Sourcing.

## vNext Version Objectives

1. Improve Command Pipeline Performance
1. Implement CommandResult Errors As Per RFC 7807
1. Use ValueTask Over Task To Better Support Synchronous Operations
1. Improve Query Pipeline Performance
1. Update To .Net 10
1. Add Benchmarks

## Tasks Remaining

1. Update readme
1. Document breaking changes and how to move from Innovation to Innovation.vNext
1. Create wiki
1. Code review
1. Profile and improve performance of the dispatcher pipeline for both commands and queries

## Alpha Warning

Please note, at this point this is a work in progress, therefore it is considered alpha grade software.
This code base and the api surface may still change.

## External Dependencies

1. MiniValidation by Damian Edwards [Link](https://github.com/DamianEdwards/MiniValidation). This replaces the outdated self written recursive validator

## Dispatcher Command Pipeline

Commands: Dispatcher -> Command Reactors -> Command Interceptors -> Command Validators -> Command Handler -> Command Result Reactors -> Audit Store -> Return Result

## Framework Components

### Command Reactors

Command Reactors Are The First Step In The Command Dispatching Pipeline.
They Can Be Used As Example For Logging Or To Prime Other Services About An Impending Command.

While The Command Is Passed In By Reference, It Is Not Advised To Edit The Object.
The Command Reactor Has No Influence Over Pipeline Execution

These Are Run In Parallel On A Background Thread

### Command Interceptors

This Is The Second Step In The Command Dispatching Pipeline.
These Can, Where Required Make Changes To A Command Or Its Properties. They Are Called One After The Other,
Not In Parallel.

### Command Validators

To Allow Better Seperation Of Concerns This Is The Third Step In The Command Dispatch Pipeline. 
Command Validators Can Be Implemented For A Given Command. It Can Validate As Required, If Validation Fails The Command Handler
Will Not Be Called, Instead The Result Of The Validation Will Be Returned.

While There Can Be Multiple Implementations, The Pipeline Will Return After The First Implementation Returns An Error

### Command Result Reactors

Command Result Reactors Are The Final Step In The Command Dispatching Pipeline.
The Can Be Used As Example For Logging Or Auditing. The Command Result Reactor Has No Influence Over Pipeline Execution

These Are Run In Parallel On A Background Thread

### Commands

Command Should Be Used To Alter State In Resources.
Commands Must Implement The ICommand Interface.

There Can Only Be A Single Handler For A Command

### Queries

Queries Are Used To Load Resources.
Queries Must Implement The IQuery Interface.

There Can Only Be A Single Handler For A Query

### Query Results

QueryResult Are Objects Which Are Returned From A Query Handler.
These Objects Must Implement The IQueryResult Interface.
This Interface Is Soley For Tracking Within The Framework And Does Not Impose Any
Field Or Property Requirements.

### Command Validation

If Commands Do Not Have Implementations Of Command Handlers Registered, They Will Be Checked
Firstly By The Microsoft Validator (System.ComponentModel.DataAnnotations.Validator), They Will 
Also Be Checked If They Implement IValidatableObject (System.ComponentModel.DataAnnotations.IValidatableObject).
If Validation Fails The Command Handler Will Not Be Called, Instead The Result Of The Validation Will Be Returned.

### Audit Store

The Framework Supports Centralised Auditing, Where Any Command, Query Or Meassage Can Be Logged.
Implement The IAuditStore Interface, And Reqister With Dependency Injection. If This Interface Is Found,
The Methods Will Be Called. If It Is Not Present, It Is Simply Ignored

### Messages

Messages Can Be Used To Broadcast To Multiple Handlers

### Correlation

The Dispatcher Supports Either Creating Its Own Or Being Supplied With A Correlation Id.
An Implementation Of This Is Available For Asp.Net Core, Using The Well Known `X-Correlation-ID` Header

Command And Query Handlers Can Now Implement The ICorrelationAware Interface.
When The Dispatcher Sees That They Implement This Interface, It Will Set The CorrelationId Before Calling The Handle Method.

### SearchLocations

The Innovation Loader Is Capable Of Loading Assemblies From Specified Locations.
This Is To Support A Modular Approach.

## Dispatcher Context



## Supported .Net Framework

1. .Net 10.x

## Samples

There are two samples. `Innovation.Sample.Console` and `Innovation.Sample.Web`

## Tests

There is a single test project, however there are two other projects in the test directory.
This is to ensure that the dynamic loading capability can be correctly tested.

## Building

In order to build the solution, you will need to following items

1. Visual Studio 2026 >= 18.8.1
3. Latest .Net10 SDK And Runtime [Download Link](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

## Benchmark Results

1 second = 1 000 ms (milliseconds)
1 second = 1 000 000 us (microseconds)
1 second = 1 000 000 000 ns (nanoseconds)

### A benchmark class to test the performance of the DataAnnotationsValidator with a BlankCommand.
| Method          | Mean     | Error    | StdDev   | Gen0   | Allocated |
|---------------- |---------:|---------:|---------:|-------:|----------:|
| BlankCommandNew | 43.02 ns | 0.110 ns | 0.092 ns | 0.0023 |      24 B |

Operations per second: 1 000 000 000 / 43.02 = 23 245 002

### A benchmark class to test the performance of the DataAnnotationsValidator with a specific command object (InsertCustomer).
| Method                | Mean     | Error   | StdDev  | Gen0   | Allocated |
|---------------------- |---------:|--------:|--------:|-------:|----------:|
| InsertCustomerCommand | 705.8 ns | 2.58 ns | 2.29 ns | 0.1144 |   1.17 KB |

Operations per second: 1 000 000 000 / 705.8 = 1 416 831

### A benchmark class to test the performance of the Dispatcher with a specific command object (BlankCommand).
| Method               | Mean     | Error   | StdDev  | Gen0   | Allocated |
|--------------------- |---------:|--------:|--------:|-------:|----------:|
| DispatchBlankCommand | 224.6 ns | 2.62 ns | 2.19 ns | 0.0134 |     144 B |

Operations per second: 1 000 000 000 / 224.6 = 4 452 359