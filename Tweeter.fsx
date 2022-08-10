#r "nuget: Akka.FSharp"
#r "nuget: MathNet.Numerics.FSharp"
#nowarn
open System
open System.Security.Cryptography
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.Diagnostics
open Akka
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open MathNet.Numerics.Distributions
open MathNet.Numerics.Random
open System
open System.Collections.Generic
open System.Data
open System.Net


let system = ActorSystem.Create("system", Configuration.defaultConfig())
let totalNumberOfUsers = fsi.CommandLineArgs.[1] |> int
let stringLength = fsi.CommandLineArgs.[2] |> int
let maxNumberOfTweets = fsi.CommandLineArgs.[3] |> int 
let mutable user_ID: int = 1
let mutable newTweets_ID = 1
let mutable reTweet_ID = 1
let UsersOnline = new Dictionary<string, bool>()
let randomNum = System.Random()
let followers = Array.create totalNumberOfUsers 0
let zipf1 = new Zipf(1.0,totalNumberOfUsers-1)
zipf1.Samples(followers)
let timer = Diagnostics.Stopwatch()
for i in 0..totalNumberOfUsers-1 do
    printfn "Number of Followers for user %A : %A" i followers.[i]
let twitterHashtags = [|
                "#coffee";
                "#NYC";
                "#cooking";
                "#writing";
                "#socialmedia";
                "#photography";
                "#followforfollowback";
                "#fridayfeeling";
                "#MondayMotivation";
                "#fintech";
                "#traveltuesday";
                "#blessed";
                "#likeforlikes";
                "#goals";
                "#fitness";
                "#artist";
                "#science";
                "#nature";
                "#sky";
                "#instatravel";
                "#holiday";
                "#quotes";
                "#tourism";
                "#sportscars";
                "#shoutoutforshoutout";
                "#clearskies";
                "#vacationtime";
                "#mood";
                "#wanderer";
                "#holidayseason";
                "#technology";
                "#gadgets";
                "#google";
                "#androidographer";
                "#smartphone";
                "#tweegram";
                "#textbooks";
                "#songs";
                "#classmates";
                "#homework";
                "#computers";
                "#compassion";
                "#eBay";
                "#sezzle";
                "#twitterlove";
                |]

type SystemMessages =
    | NewUserRegistration of string
    | NewTweetRegistration of string*string
    | NewTweetCreation of string*string
    | Subscribe of string*string
    | DisplayFeeds of string
    | DisplayPosts of string
    | DisplayTweets of string
    | ReTweets of string*string 
    | StartSimulation of string*int
    | EngineConnection of string
    | TweetCreation
    | ViewFeeds
    | AcquireTweets of string
    | UserRegistration
    | UserAction
    | SignOut
    | SignIn
    | HashtagTweetCreation
    | MentionTweetCreation
    | UserSignOut of int
    | UserSignIn of int
    | SubscribeToAnotherUser
    | RegisterFollowerToUser of string*string
    | MentionTweetQuerying
    | HashtagTweetQuerying
    | AcquireMentionedTweets of string
    | AcquireHashtagTweets of string
    | ReTweetRegistration of string*string
    | RandomReTweetCreation

// Creating the databases

let tableOfUsers = new DataTable()

tableOfUsers.Columns.Add("User_ID", typeof<string>);
tableOfUsers.Columns.Add("Username", typeof<string>);

let registerUser(id: string, username: string) = 
    let user = tableOfUsers.NewRow()
    user.SetField("User_ID", id)
    user.SetField("Username", username)
    tableOfUsers.Rows.Add(user)

let tableOfNewTweets = new DataTable()

tableOfNewTweets.Columns.Add("Tweet_ID", typeof<string>)
tableOfNewTweets.Columns.Add("User_ID", typeof<string>)
tableOfNewTweets.Columns.Add("TweetInfo", typeof<string>)

let registerNewTweet(tid: string, uid: string, newTweetsContent: string) = 
    let newTweets = tableOfNewTweets.NewRow()
    newTweets.SetField("Tweet_ID", tid)
    newTweets.SetField("User_ID", uid)
    newTweets.SetField("TweetInfo", newTweetsContent)
    tableOfNewTweets.Rows.Add(newTweets)

let tableOfNewReTweets = new DataTable()

tableOfNewReTweets.Columns.Add("ReTweet_ID", typeof<string>)
tableOfNewReTweets.Columns.Add("Tweet_ID", typeof<string>)
tableOfNewReTweets.Columns.Add("TweetInfo", typeof<string>)

let registerNewReTweets(uid: string, tid: string) = 
    let renewTweets = tableOfNewReTweets.NewRow()
    renewTweets.SetField("ReTweet_ID", uid)
    renewTweets.SetField("Tweet_ID", tid)
    let newTweets = tableOfNewTweets.Select("Tweet_ID = "+tid)
    let newTweetsRow = seq {yield! newTweets}
    renewTweets.SetField("TweetInfo","newTweetsRow.[0]")
    tableOfNewReTweets.Rows.Add(renewTweets)

let tableOfFollowers = new DataTable()
tableOfFollowers.Columns.Add("uid", typeof<string>)
tableOfFollowers.Columns.Add("fid", typeof<string>)

let registerFollower(uid: string, fid: string) = 
    let follower = tableOfFollowers.NewRow()
    follower.SetField("uid", uid)
    follower.SetField("fid", fid)
    tableOfFollowers.Rows.Add(follower)

// Random Tweet Generation

let createRandomTweet() = 
    let charList ="abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWUXYZ0123456789"
    let mutable str = ""
    let mutable index = 0
    let randomNum = System.Random()

    for i in 1 .. stringLength do
        index <- (randomNum.Next() % 62)
        str <- String.concat
                        ""
                        [str; charList.[index].ToString()]
    str <- String.concat "" [[|"ufid.1;";"ufid.2;";"ufid.3;";"ufid.4;";"ufid.5;";"ufid.6;"|].[randomNum.Next()%2];str]

    str

// Fetching from the Database

let getFollowersList(uid: string) =
    let mutable followersList = [||]
    let followersRow = tableOfFollowers.Select("uid="+uid)
    let followersData = seq { yield! followersRow}
    for follower in followersData do
        let y = [|follower.[1]|]
        followersList <- Array.append y followersList
    followersList


let feedGeneration(uid: string) =
    let followersList = getFollowersList(uid)
    let mutable newTweetsList = [||]
    for follower in followersList do
        let mutable followersTweet = tableOfNewTweets.Select("User_ID = "+(follower|>string))
        let followersTweetInfo = seq {yield! followersTweet}
        for newTweets in followersTweetInfo do
            newTweetsList <- Array.append [|newTweets.[2]|] newTweetsList
    newTweetsList

let getQueriedTweets(pattern: string) =
    let mutable newTweetsList = [||]
    let mutable newTweetsData = tableOfNewTweets.Select()
    let newTweets = seq {yield! newTweetsData}
    for newTweets in newTweets do
        let newTweetstring = newTweets.[2] |> string
        if newTweetstring.Contains(pattern) then
            newTweetsList <- Array.append [|newTweets.[2]|] newTweetsList
    newTweetsList

// Engine Actor

let TwitterEngine(mailbox: Actor<_>) =
    let rec loop() = actor {
        let! message = mailbox.Receive()
        match message with
        | NewUserRegistration(username) ->
            user_ID <- user_ID + 1
            registerUser(user_ID |> string, username)
            mailbox.Sender() <! user_ID
        | NewTweetRegistration(user_ID, newTweets) ->
            if (user_ID |> string) < (newTweets_ID |> string) then
                let x = ((newTweets_ID |> int)  + 1) |> string
                registerNewTweet(x, (newTweets_ID |> string),newTweets)
            else
                newTweets_ID <- newTweets_ID + 1
                registerNewTweet(newTweets_ID |> string, user_ID |> string, newTweets)
        | UserSignIn(user_ID) ->
            UsersOnline.[user_ID |> string] <- true
        | UserSignOut(user_ID) ->
            UsersOnline.[user_ID |> string] <- false
        | AcquireTweets(user_ID) ->
            let newTweetsData = feedGeneration(user_ID)
            mailbox.Sender() <! newTweetsData
        | RegisterFollowerToUser(user_ID, followerID) ->
            registerFollower(user_ID, followerID)
        | AcquireMentionedTweets(pattern) ->
            let newTweets = getQueriedTweets(pattern)
            ()
        | AcquireHashtagTweets(pattern) ->
            let newTweets = getQueriedTweets(pattern)
            ()
        | ReTweetRegistration(user_ID, newTweets_ID) ->
            if user_ID < newTweets_ID then
                let x = ((newTweets_ID |> int)  + 1) |> string
                registerNewReTweets(x, newTweets_ID)
            else
                registerNewReTweets(user_ID, newTweets_ID)
        | _ -> ()
        return! loop()
    }
    loop()

let twitterSimulator = spawn system "TwitterEngine" TwitterEngine

// Actor for Simulation

let TwitterClient(mailbox: Actor<_>) =
    let mutable signoutUser = 1
    let mutable currentReTweets = 0
    let maxReTweets = 10
    let mutable username = null
    let mutable user_IDP = 0
    let mutable maximumNumberOfFollowers = 0
    let mutable activeFollowers = 0
    let maximumNumberOfTweets = 20
    let mutable currentTweets = 0
    let rec loop() = actor {
        let! message = mailbox.Receive()
        match message with 
        | StartSimulation(uname, followers) ->
            username <- uname
            maximumNumberOfFollowers <- followers
            mailbox.Self <! UserRegistration
        | UserRegistration ->
            user_IDP <- Async.RunSynchronously(twitterSimulator <? NewUserRegistration(username))
            UsersOnline.[user_IDP |> string] <- true
            //printfn "Registering user: %A" mailbox.Self.Path.Name
            //mailbox.Self <! UserAction
        | UserAction ->
            timer.Start()
            let userCommands = [|
                                TweetCreation;
                                ViewFeeds;
                                SignOut;
                                SignIn;
                                MentionTweetCreation;
                                HashtagTweetCreation;
                                SubscribeToAnotherUser;
                                MentionTweetQuerying;
                                HashtagTweetQuerying;
                                RandomReTweetCreation
                                |]
            let x = randomNum.Next()%userCommands.Length
            //printfn "%A performing -> %A" mailbox.Self.Path.Name userCommands.[x]
            mailbox.Self <! userCommands.[x]
        | TweetCreation ->
            if currentTweets < maximumNumberOfTweets then
                if UsersOnline.[user_IDP |> string] then
                    let newTweets = createRandomTweet()
                    twitterSimulator <! NewTweetRegistration(user_ID |> string, newTweets)
                    currentTweets <- currentTweets + 1
            mailbox.Self <! UserAction
        | SubscribeToAnotherUser ->
            if activeFollowers < maximumNumberOfFollowers then
                let mutable subscribeToID = randomNum.Next()%(totalNumberOfUsers + 1)
                while subscribeToID = user_IDP do
                    subscribeToID <- randomNum.Next()%(totalNumberOfUsers + 1)
                
                twitterSimulator <! RegisterFollowerToUser(user_IDP |> string,
                                                        subscribeToID |> string)
                //printfn "%A -> subscribed ->%A" user_IDP subscribeToID
                activeFollowers <- activeFollowers + 1

            mailbox.Self <! UserAction
        | HashtagTweetCreation ->
            if currentTweets < maximumNumberOfTweets then
                if UsersOnline.[user_IDP |> string] then
                    let mutable randomHashtag = ""
                    let mutable newTweets = createRandomTweet()
                    newTweets <- newTweets + " " + randomHashtag
                    twitterSimulator <! NewTweetRegistration(user_ID |> string, newTweets)
                    currentTweets <- currentTweets + 1
            mailbox.Self <! UserAction
        | MentionTweetCreation ->
            if currentTweets < maximumNumberOfTweets then
                if UsersOnline.[user_IDP |> string] then
                    let mutable num = randomNum.Next()%(totalNumberOfUsers + 1)
                    let mutable mention = "@TwitterClient" + (num |> string)
                    let mutable randomMention = mention
                    let mutable newTweets = createRandomTweet()
                    newTweets <- newTweets + " " + randomMention
                    twitterSimulator <! NewTweetRegistration(user_ID |> string, newTweets)
                    currentTweets <- currentTweets + 1
            mailbox.Self <! UserAction
        | RandomReTweetCreation -> 
            if currentReTweets < maxReTweets then 
                currentReTweets <- currentReTweets + 1
                twitterSimulator <! ReTweetRegistration(user_IDP |> string, (randomNum.Next()%(newTweets_ID+1))|>string)
            mailbox.Self <! UserAction
        | ViewFeeds ->
            if UsersOnline.[user_IDP |> string] then
                let response = Async.RunSynchronously(
                                    twitterSimulator <? AcquireTweets(user_IDP |> string))
                ()
            mailbox.Self <! UserAction
        | SignOut ->
            twitterSimulator <! UserSignOut(user_IDP)
            signoutUser <- signoutUser + 1
            timer.Stop()
            printfn "%A I am Signing Out after %i ms" mailbox.Self timer.ElapsedMilliseconds
            if signoutUser = totalNumberOfUsers then 
                printf "System Terminated!"
                mailbox.Context.System.Terminate() |> ignore
        | SignIn ->
            twitterSimulator <! UserSignIn(user_IDP)
            mailbox.Self <! UserAction
        | MentionTweetQuerying ->
            twitterSimulator <! AcquireMentionedTweets("@TwitterClient"+(user_ID|>string))
            mailbox.Self <! UserAction
        | HashtagTweetQuerying ->
            twitterSimulator <! AcquireHashtagTweets(twitterHashtags.[randomNum.Next()%twitterHashtags.Length])
            mailbox.Self <! UserAction
        | _ -> ()
        return! loop()
    }
    loop()


let spawnSystem =
    // Start the stopwatch
    timer.Start()
    let clients = [1..totalNumberOfUsers]
                    |> List.map(fun client ->
                                    spawn system ("user"+(client |> string)) TwitterClient)
    printfn "The clients are : %A" clients
    for i in 0..totalNumberOfUsers-1 do
        clients.[i] <! StartSimulation(
                            "TwitterClient" + ((i+1) |> string),
                            followers.[i]
                        )
    timer.Stop()
    printfn "Time taken = %i ms\n" timer.ElapsedMilliseconds
    printfn "Users Online : %A" UsersOnline
    for i in 0..totalNumberOfUsers-1 do
        clients.[i] <! UserAction
    
spawnSystem
Console.ReadLine() |> ignore