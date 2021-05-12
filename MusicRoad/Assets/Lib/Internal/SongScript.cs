using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Numerics;
using DSPLib;

[RequireComponent(typeof(MeshFilter))]
public class SongScript : MonoBehaviour
{

	public AudioSource audioSource;
	SpectrumAnalyzer preProcessedSpectralFluxAnalyzer;
	public GameObject cameraObject;

	int numChannels;
	int numTotalSamples;
	int spectrumSampleSize = 2048;
	int sampleRate;
	float curSongTime;
	float[] multiChannelSamples;
	int clipLength;
	
	float[] preProcessedSamples;

	List<Transform> spectrumPosition = new List<Transform>();
	List<Transform> cameraPositions = new List<Transform>();

	private int curveCount = 0;

	bool startgame = false;
	bool fftdone = false;
	bool spawnedobjects = false;
	public bool startmusic = false;

	[SerializeField] Mesh2D shape2D;
	int roadPlaneCount = 6;
	Mesh mesh;

	private bool coroutineAllowed;
	float cameraT = 0f;

	public GameObject audiodata;
	public List<GameObject> basspoints;

	// Start is called before the first frame update
	void Awake()
	{
		preProcessedSpectralFluxAnalyzer = new SpectrumAnalyzer();
		mesh = new Mesh();
		mesh.name = "Segment";
		GetComponent<MeshFilter>().sharedMesh = mesh;

		// Need all audio samples.  If in stereo, samples will return with left and right channels interweaved
		// [L,R,L,R,L,R]

		GameObject audio = GameObject.FindGameObjectWithTag("AUDIOSOURCE");
		audioSource = audio.GetComponent<AudioSource>();

		multiChannelSamples = new float[audioSource.clip.samples * audioSource.clip.channels];
		numChannels = audioSource.clip.channels;
		numTotalSamples = audioSource.clip.samples;
		clipLength = (int)audioSource.clip.length;

		// We are not evaluating the audio as it is being played by Unity, so we need the clip's sampling rate
		this.sampleRate = audioSource.clip.frequency;

		audioSource.clip.GetData(multiChannelSamples, 0);
		Debug.Log("GetData done");

		//Thread bgThread = new Thread(this.getFullSpectrumThreaded);
		//bgThread.Start();
		getFullSpectrumThreaded();
		//Thread th = new Thread(this.testing);
		//th.Start();

		if (fftdone == true)
		{
			plottingPoints(preProcessedSpectralFluxAnalyzer.spectralFluxSamples);
			MeshGenerator();
            if (startgame == true)
            {
				Debug.Log("outputdata length" + preProcessedSamples.Length);
				Debug.Log("sample rate" + sampleRate);
				startmusic = true;
				coroutineAllowed = true;
			}
		}
	}

	void FixedUpdate()
    {

        if (coroutineAllowed == true)
        {
			StartCoroutine(CameraMovement());
			coroutineAllowed = false;
		}
	}

    //private int getIndexFromTime(float curTime)
    //{
    //	float lengthPerSample = this.clipLength / (float)this.numTotalSamples;
    //	return Mathf.FloorToInt(curTime / lengthPerSample);
    //}

    float getTimeFromIndex(int index)
	{
		return ((1f / (float)this.sampleRate) * index);
	}

	void plottingPoints(List<SpectralFluxInfo2> thresholdpoint)
	{
		float x = 0;
		for (int i = 0; i < thresholdpoint.Count; i++)
		{
			if (thresholdpoint[i].threshold != 0)
			{
				x = 0;
				GameObject p0 = (Instantiate(Resources.Load("meshpoint"), transform) as GameObject);
				GameObject camPos = (Instantiate(Resources.Load("campoint"), transform) as GameObject);
                //Debug.Log(thresholdpoint[i].threshold);

                if (thresholdpoint[i].threshold >= 0.5f)
                {
                    if (thresholdpoint[i].threshold > 1.5f)
                    {
                        x -= 40;
                    }
                    else if (thresholdpoint[i].threshold > 1f)
                    {
                        x -= 30;
                    }
                    else
                    {
                        x -= 35;
                    }
                    p0.transform.position = new UnityEngine.Vector3(x, (thresholdpoint[i].threshold * -1000f), i * 3f);
                    camPos.transform.position = new UnityEngine.Vector3(x, (thresholdpoint[i].threshold * -1000f), i * 3f);
                    spectrumPosition.Add(p0.transform);
                    cameraPositions.Add(camPos.transform);
                }
                else if (thresholdpoint[i].threshold < 0.4f || thresholdpoint[i].threshold >= 0.2f)
                {
                    x -= 20f;
                    p0.transform.position = new UnityEngine.Vector3(x, (thresholdpoint[i].threshold * -600f), i * 3f);
                    camPos.transform.position = new UnityEngine.Vector3(x, (thresholdpoint[i].threshold * -600f), i * 3f);
                    spectrumPosition.Add(p0.transform);
                    cameraPositions.Add(camPos.transform);
                }
                else if (thresholdpoint[i].threshold < 0.2f || thresholdpoint[i].threshold >= 0.1f)
                {
                    x += 14f;
                    p0.transform.position = new UnityEngine.Vector3(x, (thresholdpoint[i].threshold * -300f), i * 3f);
                    camPos.transform.position = new UnityEngine.Vector3(x, (thresholdpoint[i].threshold * -300f), i * 3f);
                    spectrumPosition.Add(p0.transform);
                    cameraPositions.Add(camPos.transform);
                }
                else
                {
                    x += 7f;
                    p0.transform.position = new UnityEngine.Vector3(x, (thresholdpoint[i].threshold * -200f), i * 3f);
                    camPos.transform.position = new UnityEngine.Vector3(x, (thresholdpoint[i].threshold * -200f), i * 3f);
                    spectrumPosition.Add(p0.transform);
                    cameraPositions.Add(camPos.transform);
                } 
                p0.SetActive(false);
				camPos.SetActive(false);
				Destroy(p0);

                if (thresholdpoint[i].isPeak == true)
                {
					GameObject point = (Instantiate(Resources.Load("basspoint"), transform) as GameObject);
					point.transform.position = new UnityEngine.Vector3(p0.transform.position.x, p0.transform.position.y + 2, i * 3f);
					basspoints.Add(point);
					Debug.Log(i);
					Debug.Log(thresholdpoint[i].spectralFlux);
				}
			}
		}
		//cameraObject.transform.position = cameraPositions[0].position;
		Debug.Log("spectrum points: " + spectrumPosition.Count);
		spawnedobjects = true;
	}

	IEnumerator CameraMovement()
	{ 
		curveCount = (int)cameraPositions.Count / 3;
        for (int i = 0; i < curveCount; i++)
        {
            int cameraindex = i * 3;
            while (cameraT < 1)
			{
				yield return new WaitForEndOfFrame();
				cameraT += (audiodata.GetComponent<AudioData_AmplitudeBand>()._CurrentAmplitude * (audiodata.GetComponent<AudioData_AmplitudeBand>()._freqBand[0] + audiodata.GetComponent<AudioData_AmplitudeBand>()._freqBand[1] + audiodata.GetComponent<AudioData_AmplitudeBand>()._freqBand[2]) * Time.fixedDeltaTime);
				OrientedPoint cameraBP = GetBezierPointOP(cameraPositions[cameraindex].position, cameraPositions[cameraindex + 1].position, cameraPositions[cameraindex + 2].position, cameraPositions[cameraindex + 3].position, cameraT);
				cameraObject.transform.position = cameraBP.point;
				yield return new WaitForEndOfFrame();
				cameraObject.transform.eulerAngles = cameraBP.rot.eulerAngles;
				yield return new WaitForEndOfFrame();

			}
			cameraT = 0f;
			yield return new WaitForFixedUpdate();
		}
		coroutineAllowed = false;
	}


	private void getFullSpectrumThreaded()
	{
		try
		{
			// We only need to retain the samples for combined channels over the time domain
			preProcessedSamples = new float[this.numTotalSamples];

			int numProcessed = 0;
			float combinedChannelAverage = 0f;
			for (int i = 0; i < multiChannelSamples.Length; i++)
			{
				combinedChannelAverage += multiChannelSamples[i];

				// Each time we have processed all channels samples for a point in time, we will store the average of the channels combined
				if ((i + 1) % this.numChannels == 0)
				{
					preProcessedSamples[numProcessed] = combinedChannelAverage / this.numChannels;
					numProcessed++;
					combinedChannelAverage = 0f;
				}
			}

			Debug.Log("Combine Channels done");
			//Debug.Log(preProcessedSamples.Length);

			// Once we have our audio sample data prepared, we can execute an FFT to return the spectrum data over the time domain
			spectrumSampleSize = 2048;
			int iterations = preProcessedSamples.Length / spectrumSampleSize;
			//Debug.Log("itterations: " + iterations);
			FFT fft = new FFT();
			fft.Initialize((UInt32)spectrumSampleSize);

			//Debug.Log(string.Format("Processing {0} time domain samples for FFT", iterations));
			double[] sampleChunk = new double[spectrumSampleSize];
			for (int i = 0; i < iterations; i++)
			{
				// Grab the current 1024 chunk of audio sample data
				Array.Copy(preProcessedSamples, i * spectrumSampleSize, sampleChunk, 0, spectrumSampleSize);

				// Apply our chosen FFT Window
				double[] windowCoefs = DSP.Window.Coefficients(DSP.Window.Type.Welch, (uint)spectrumSampleSize);
				double[] scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefs);
				double scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefs);

				// Perform the FFT and convert output (complex numbers) to Magnitude
				Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
				double[] scaledFFTSpectrum = DSPLib.DSP.ConvertComplex.ToMagnitude(fftSpectrum);
				scaledFFTSpectrum = DSP.Math.Multiply(scaledFFTSpectrum, scaleFactor);

				// These 1024 magnitude values correspond (roughly) to a single point in the audio timeline
				curSongTime = getTimeFromIndex(i) * spectrumSampleSize;

				// Send our magnitude data off to our Spectral Flux Analyzer to be analyzed for peaks
				preProcessedSpectralFluxAnalyzer.analyzeSpectrum(Array.ConvertAll(scaledFFTSpectrum, x => (float)x), curSongTime);
			}

			//Debug.Log("Spectrum Analysis done");
			//Debug.Log("Background Thread Completed");
			Debug.Log("Spectral Sample Count: " + preProcessedSpectralFluxAnalyzer.spectralFluxSamples.Count);
			fftdone = true;
		}

		catch (Exception e)
		{
			// Catch exceptions here since the background thread won't always surface the exception to the main thread
			Debug.Log(e.ToString());
		}
	}

    void MeshGenerator()
	{
		if (fftdone == true && spawnedobjects == true)
		{
			mesh.Clear();
			Debug.Log("Curve Count: " + curveCount);
			float uSpan = shape2D.CalcUspan();
			List<UnityEngine.Vector3> verts = new List<UnityEngine.Vector3>();
			List<UnityEngine.Vector3> normals = new List<UnityEngine.Vector3>();
			List<UnityEngine.Vector2> uvs = new List<UnityEngine.Vector2>();
			List<int> triangles = new List<int>();
			curveCount = (int)spectrumPosition.Count / 3;

			for (int j = 0; j < curveCount; j++)
			{
				int nodeIndex = j * 3;
				int p0 = nodeIndex;
				int p1 = nodeIndex + 1;
				int p2 = nodeIndex + 2;
				int p3 = nodeIndex + 3;
				//Debug.Log("p0: " + p0);
				//Debug.Log("p1: " + p1);
				//Debug.Log("p2: " + p2);
				//Debug.Log("p3: " + p3);

				for (int i = 0; i < roadPlaneCount; i++)
				{
					float t = i / ((float)roadPlaneCount - 1f);
					OrientedPoint op = GetBezierPointOP(spectrumPosition[p0].position, spectrumPosition[p1].position, spectrumPosition[p2].position, spectrumPosition[p3].position, t);
					//Handles.DrawBezier(spectrumPosition[nodeIndex].position, spectrumPosition[nodeIndex + 3].position, spectrumPosition[nodeIndex + 1].position, spectrumPosition[nodeIndex + 2].position, Color.blue, null, 2f);
					//Handles.PositionHandle(op.point, op.rot);
					for (int k = 0; k < shape2D.VertexCount; k++)
					{
						verts.Add(op.LocalToWorldPos(shape2D.vertices[k].points));
						normals.Add(op.LocalToWorldVector(shape2D.vertices[k].normal));
						uvs.Add(new UnityEngine.Vector2(shape2D.vertices[k].U, t * GetApproxLength(spectrumPosition[nodeIndex].position, spectrumPosition[nodeIndex + 1].position, spectrumPosition[nodeIndex + 2].position, spectrumPosition[nodeIndex + 3].position) / uSpan));
					}

					//Gizmos.color = Color.green;
					//UnityEngine.Vector3[] localVerts = shape2D.vertices.Select(v => op.LocalToWorldPos(v.points)).ToArray();

					//for (int k = 0; k < shape2D.LineCount; k += 2)
					//{
					//	UnityEngine.Vector3 a = localVerts[shape2D.lineIndices[k]];
					//	UnityEngine.Vector3 b = localVerts[shape2D.lineIndices[k + 1]];
					//	Gizmos.DrawLine(a, b);
					//}

				}

				for (int i = 0; i < roadPlaneCount - 1; i++)
				{

					int Testing = (i + (j * roadPlaneCount)) * shape2D.VertexCount;
					int TestingNext = (i + (j * roadPlaneCount) + 1) * shape2D.VertexCount;

					for (int k = 0; k < shape2D.LineCount; k += 2)
					{
						int lineA = shape2D.lineIndices[k];
						int lineB = shape2D.lineIndices[k + 1];

						int currentA = Testing + lineA;
						int currentB = Testing + lineB;
						//Debug.Log(currentA);
						//Debug.Log(currentB);

						int nextA = TestingNext + lineA;
						int nextB = TestingNext + lineB;
						////Debug.Log(nextA);
						////Debug.Log(nextB);


						////makes the first segment
						//Gizmos.DrawLine(verts[lineA], verts[lineB]);

						//////makes the second and third segment
						//Gizmos.DrawLine(verts[currentA], verts[currentB]);

						//////makes the last three
						//Gizmos.DrawLine(verts[nextA], verts[nextB]);

						////criscross
						//Gizmos.DrawLine(verts[currentA], verts[nextA]);
						//Gizmos.DrawLine(verts[nextB], verts[currentA]);
						//Gizmos.DrawLine(verts[nextB], verts[currentB]);

						//Gizmos.DrawLine(verts[nextA], verts[nextA2]);
						//Gizmos.DrawLine(verts[nextB2], verts[nextA]);
						//Gizmos.DrawLine(verts[nextB2], verts[nextB]);

						//Gizmos.DrawLine(verts[nextA2], verts[nextA3]);
						//Gizmos.DrawLine(verts[nextB3], verts[nextA2]);
						//Gizmos.DrawLine(verts[nextB3], verts[nextB2]);

						triangles.Add(currentA);
						triangles.Add(nextA);
						triangles.Add(nextB);
						triangles.Add(currentA);
						triangles.Add(nextB);
						triangles.Add(currentB);
                    }
				}
			}

			mesh.SetVertices(verts);
			mesh.SetNormals(normals);
			mesh.SetUVs(0, uvs);
			mesh.SetTriangles(triangles, 0);
			GetComponent<MeshCollider>().sharedMesh = mesh;
			startgame = true;
		}
	}

	OrientedPoint GetBezierPointOP(UnityEngine.Vector3 startPosition, UnityEngine.Vector3 position1, UnityEngine.Vector3 position2, UnityEngine.Vector3 endbezierPosition, float t)
	{
		UnityEngine.Vector3 p0 = startPosition;
		UnityEngine.Vector3 p1 = position1;
		UnityEngine.Vector3 p2 = position2;
		UnityEngine.Vector3 p3 = endbezierPosition;

		UnityEngine.Vector3 a = UnityEngine.Vector3.Lerp(p0, p1, t);
		UnityEngine.Vector3 b = UnityEngine.Vector3.Lerp(p1, p2, t);
		UnityEngine.Vector3 c = UnityEngine.Vector3.Lerp(p2, p3, t);

		UnityEngine.Vector3 d = UnityEngine.Vector3.Lerp(a, b, t);
		UnityEngine.Vector3 e = UnityEngine.Vector3.Lerp(b, c, t);

		UnityEngine.Vector3 pos = UnityEngine.Vector3.Lerp(d, e, t);
		UnityEngine.Vector3 tangent = (e - d).normalized;

		return new OrientedPoint(pos, tangent);
	}

	float GetApproxLength(UnityEngine.Vector3 startPosition, UnityEngine.Vector3 position1, UnityEngine.Vector3 position2, UnityEngine.Vector3 endbezierPosition)
    {
        int precision = 4;
        UnityEngine.Vector3[] points = new UnityEngine.Vector3[precision];

        for (int i = 1; i < precision; i++)
        {
            float t = i / precision;
            points[i] = GetBezierPointOP(startPosition, position1, position2, endbezierPosition, t).point;
        }
        float distance = 0;
        for (int i = 0; i < precision - 1; i++)
        {
			UnityEngine.Vector3 a = points[i];
			UnityEngine.Vector3 b = points[i + 1];
            distance = UnityEngine.Vector3.Distance(a, b);
        }
        return distance;
    }
}

