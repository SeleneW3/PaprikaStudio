using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraController : MonoBehaviour 
{
	public List<Camera> Cameras = new List<Camera>();
	
	private int m_CurrentIndex = 0;
	
	public void Awake()
	{
		for( int i = 0; i < Cameras.Count; i ++ )
		{
			Cameras[i].enabled = false;
			if( i == m_CurrentIndex )
			{
				Cameras[i].enabled = true;
			}
		}
	}
	
	void OnGUI () 
	{
		if(GUI.Button(new Rect((Screen.width/2.3f),(Screen.height/1.15f),150,40), "NEXT")) 
		{
			m_CurrentIndex ++;
			if( m_CurrentIndex >= Cameras.Count )
			{
				m_CurrentIndex = 0;
			}
			
			for( int i = 0; i < Cameras.Count; i ++ )
			{
				Cameras[i].enabled = false;
				if( i == m_CurrentIndex )
				{
					Cameras[i].enabled = true;
				}
			}
		}
	}
}
