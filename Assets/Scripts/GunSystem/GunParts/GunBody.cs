using UnityEngine;

[CreateAssetMenu(fileName = "GunBody", menuName = "Gun Parts/Gun Body")]
public class GunBodyPart : GunPart
{
	[Header("Gun Body")]
	public GunBodyType bodyType = GunBodyType.Pistol;

	private void OnEnable()
	{
		partType = GunPartType.GunBody;
	}
}