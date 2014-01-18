using System;
using Newtonsoft.Json;

namespace Atlas.Models.Github
{
	public class PluginRevisionHistory : GitHubRequest
	{
		public string Sha { get; set; }

		public CommitEntry Commit { get; set; }

		public string Url { get; set; }

		[JsonProperty("html_url")]
		public string HtmlUrl { get; set; }

		[JsonProperty("comments_url")]
		public string CommentsUrl { get; set; }

		public GithubUser Author { get; set; }

		public GithubUser Committer { get; set; }

		public ParentEntry[] Parents { get; set; }

		public class CommitEntry
		{
			public GitUserDetails Author { get; set; }

			public GitUserDetails Committer { get; set; }

			public string Message { get; set; }

			public CommitTree Tree { get; set; }

			public string Url { get; set; }

			[JsonProperty("comment_count")]
			public int CommentCount { get; set; }

			public class GitUserDetails
			{
				public string Name { get; set; }

				public string Email { get; set; }

				public DateTime Date { get; set; }
			}

			public class CommitTree
			{
				public string Sha { get; set; }

				public string Url { get; set; }
			}
		}

		public class GithubUser
		{
			public string Login { get; set; }

			public int Id { get; set; }

			[JsonProperty("avatar_url")]
			public string AvatarUrl { get; set; }

			[JsonProperty("gravatar_id")]
			public string GravatarUrl { get; set; }

			public string Url { get; set; }

			[JsonProperty("html_url")]
			public string HtmlUrl { get; set; }

			[JsonProperty("followers_url")]
			public string FollowersUrl { get; set; }

			[JsonProperty("following_url")]
			public string FollowingUrl { get; set; }

			[JsonProperty("gists_url")]
			public string GistsUrl { get; set; }

			[JsonProperty("starred_url")]
			public string StarredUrl { get; set; }

			[JsonProperty("subscriptions_url")]
			public string SubscriptionsUrl { get; set; }

			[JsonProperty("organizations_url")]
			public string OrganizationsUrl { get; set; }

			[JsonProperty("repos_url")]
			public string ReposUrl { get; set; }

			[JsonProperty("events_url")]
			public string EventsUrl { get; set; }

			[JsonProperty("received_events_url")]
			public string ReceivedEventsUrl { get; set; }

			public string Type { get; set; }

			[JsonProperty("site_admin")]
			public bool SiteAdmin { get; set; }
		}

		public class ParentEntry
		{
			public string Sha { get; set; }

			public string Url { get; set; }

			public string HtmlUrl { get; set; }
		}
	}
}
