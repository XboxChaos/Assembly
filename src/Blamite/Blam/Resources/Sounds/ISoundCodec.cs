using System;

namespace Blamite.Blam.Resources.Sounds
{
	public enum Channel
	{
		Mono,
		Stereo
	}

	public interface ISoundPlatformCodec
	{
		Channel Channel { get; }
	}
}
