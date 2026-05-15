import React from 'react'
import AudioPlayer from './components/AudioPlayer'
import sampleTranscript from './data/10704443-monologue.json'

import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap-icons/font/bootstrap-icons.css';
import './assets/css/main.css'


const App = () => {
  const audioUrl = "https://pcimicroservices.publishingconcepts.com/NLUWebAPI/Proxy.ashx/my.wav?url=https://ohpcdn.publishingconcepts.com/OHP/2025-08-25/OHP_KansasUniv2025_4900_20250825-122328_6203230254_46305831_10704443-RID41-all.mp3";
  //const audioUrl = "https://pcimicroservices.publishingconcepts.com/NLUWebAPI/Proxy.ashx/my.wav?url=https://ohpcdn.publishingconcepts.com/OHP/2025-09-15/OHP_SaintLouisUniv2025_4943_20250915-102318_6608886827_46932183_75799411-RID11-all.mp3";
  //const audioUrl = "https://pcimicroservices.publishingconcepts.com/NLUWebAPI/Proxy.ashx/my.wav?url=https://ohpcdn.publishingconcepts.com/OHP/2025-11-05/OHP_SaintLouisUniv2025_4943_20251105-105419_3149662661_48709683_72509974-RID7578-all.mp3";
  //const audioUrl = "https://pcimicroservices.publishingconcepts.com/NLUWebAPI/Proxy.ashx/my.wav?url=https://ohpcdn.publishingconcepts.com/OHP/2025-11-05/OHP_SaintLouisUniv2025_4943_20251105-164512_3145669215_48719474_76024078-RID8252-all.mp3";

  return (
    <div className="App">
      <nav className="navbar navbar-dark bg-primary mb-4">
        <div className="container-fluid">
          <span className="navbar-brand mb-0 h1">
            <i className="bi bi-file-earmark-music me-2"></i>
            Audio Transcript Player
          </span>
        </div>
      </nav>

      <div className="container">
        <div className="row mb-4">
          <div className="col-12">
            <div className="alert alert-info">
              <h5 className="alert-heading">
                <i className="bi bi-info-circle me-2"></i>
                Demo Application
              </h5>
              <p className="mb-0">
                This demo shows the Audio Transcript Player with word-by-word highlighting.
                Replace the <code>audioUrl</code> with test audio file URL.
              </p>
              <p><strong>URL:</strong> {audioUrl}</p>
            </div>
          </div>
        </div>

        {/*  Audio player component.  May want to split this out to just audio player and a different component for transcription.  */}
        <AudioPlayer audioUrl={audioUrl}
                     transcriptData={sampleTranscript}
                     transcriptType='monologue'
        />




        <div className="row mt-4">
          <div className="col-12">
            <div className="card">
              <div className="card-body">
                <h5 className="card-title">Features</h5>
                <ul>
                  <li><strong>Flexible Audio Sources:</strong> Works with any HTTP/HTTPS audio URL</li>
                  <li><strong>Word-by-Word Highlighting:</strong> Each word highlights as it's spoken</li>
                  <li><strong>Interactive Transcript:</strong> Click any word to jump to that point</li>
                  <li><strong>Auto-Scrolling:</strong> Transcript follows the audio automatically</li>
                  <li><strong>Responsive Design:</strong> Works on desktop, tablet, and mobile</li>
                  <li><strong>Accessibility:</strong> Full keyboard navigation and screen reader support</li>
                </ul>
              </div>
            </div>
          </div>
        </div>



      </div>
    </div>
  )
}

export default App
