using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class Lightning : MonoBehaviour 
{
	public Light TargetLight = null;
	
	public bool Enabled = false;
	
	public float MinWaitTime = 0.0f;
	public float MaxWaitTime = 0.0f;
	
	public AudioSource Sound = null;
	
	public void Awake()
	{
		if( TargetLight == null )
		{
			return;
		}
		
		if( Enabled )
		{
			StartCoroutine( PlayLightning() );
		}
	}
	
	private IEnumerator PlayLightning()
	{
		while( Enabled )
		{
			yield return new WaitForSeconds( Random.Range( MinWaitTime,MaxWaitTime ) );
			
			TargetLight.intensity = 0.0f;
			
			if( Sound != null )
			{
				Sound.Play ();
			}
			
			while( TargetLight.intensity < 0.3f )
			{
				TargetLight.intensity = Mathf.Lerp( TargetLight.intensity, 0.4f, Time.deltaTime*15.0f );
				yield return new WaitForSeconds( 0.0f );
			}
			while( TargetLight.intensity > 0.0f )
			{
				TargetLight.intensity = Mathf.Lerp( TargetLight.intensity, -0.1f, Time.deltaTime*3.0f );
				yield return new WaitForSeconds( 0.0f );
			}
			
			TargetLight.intensity = 0.0f;
		}
	}
}
