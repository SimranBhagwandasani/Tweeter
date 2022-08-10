# Tweeter
A Twitter clone to implement Twitter like functionalities using WebSocket API.

## Instructions to Run the Project
dotnet fsi project4.fsx numberOfUsers lengthOfTweet maxNumberOfTweets
 
## Functionalities/Checklist of the System
- The Twitter engine implements the functionality of   the account of the user. The user can post and send tweets which contain #hashtags and @mentions .
- One user can   to another user's tweets.
- Users can   their own tweet as well as other's tweets.
- Querying has been done on the basis of #hashtags and .
- Simulator spawns multiple actors which behaves like .
- Maintained a list of live users and pushed the feeds to them.
- The number of users and their load distribution has been determined using the Zipf distribution.

## Maximum Number of Users Simulated in the System

The maximum number of users for which the system has been tested is 11,000 when every user is having the count of maximum 10 tweets of string length 20.

## High-Level Working of the System

- The given numberOfUser actors' are spawned and loged in to behave active users in simulation. Every User starts to perform randomly selected
- UserActions are mainly-Tweet,Retweet and showFeed.
- As and when the signIn or signOut are selected from userAction randomly, the usersOnline gets updated.
- Once the actor selects signOut from list of userActions,the count of the active users descreases. After a point of time, when there are no active users remaining, the system terminates.
- Whenever any user pushes new tweet, all usersOnline gets that feed upon querying displayFeeds irrespective if they follow that particular user or not.

## Performance Analysis

- Engagement Time: Average Time spent by each user before signing out, performing various activites.
  - For 1000 users, Avergae engagement time on the system was 5.4 seconds.
  - For 11,000 users, Avergae engagement time by every actor was on the system was 6.4 seconds.
- Maximum active users vs Average Feeds per user vs Average Journey time before Signing out
![Screenshot 2022-08-10 at 1 29 01 PM](https://user-images.githubusercontent.com/74771675/183977931-1d3c353a-9067-4dec-a81c-83ea796a396b.png)

