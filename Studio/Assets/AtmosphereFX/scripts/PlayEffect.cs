using UnityEngine;
using System.Collections;

public class PlayEffect : MonoBehaviour 
{
	public GameObject Effect = null;
	public Transform Node = null;
	
	public float OverrideTime = 0.0f;
	
	private float m_Time = 0.0f;
	
	public void Awake()
	{
		m_Time = Time.time;
	}
	
	void Update()
	{
		if( Effect == null )
		{
			return;
		}
		
		if( OverrideTime > 0.0f )
		{
			if( Time.time >= m_Time+(OverrideTime) )
			{
				Instantiate( Effect, Node.position, Node.rotation );
				
				m_Time = Time.time;
			}
		}
		else if( Time.time >= m_Time+(1.5f) )
		{
			Instantiate( Effect, Node.position, Node.rotation );
				
			m_Time = Time.time;
		}
	}
}
