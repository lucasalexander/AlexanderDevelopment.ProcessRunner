# AlexanderDevelopment.ProcessRunner
This is an open source Dynamics CRM solution for scheduling and executing recurring workflows. It executes a FetchXML query to return a set of records and then start a workflow for each of those records without requiring any external processes or tools. This is a generalized approach to solving a class of problems that includes the following scenarios:

1. The birthday greetings problem: How can you, on a daily basis, send an e-mail to every contact with a birthday = today (where the date value for today is obviously different every day)?
1. The monthly update problem: How can you, on a monthly basis, generate an activity for every account with status reason = X (where it's important that the process only runs on a certain day of the month based on status reason values as of that exact date)?

For more information, see my ["Updated solution for scheduling recurring Dynamics CRM workflows"](http://alexanderdevelopment.net/post/2016/09/19/updated-solution-for-scheduling-recurring-dynamics-crm-workflows/) blog post.

The original CRM 2011 version of this solution was first described described in my ["Scheduling recurring Dynamics CRM workflows with FetchXML"](http://www.alexanderdevelopment.net/post/2013/05/19/scheduling-recurring-dynamics-crm-workflows-with-fetchxml/) blog post.