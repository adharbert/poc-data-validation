import React, { useState, useRef, useEffect } from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';

const AudioPlayer = ({ audioUrl, transcriptData, transcriptType = 'monologue' }) => {
  // State management
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const [activeWordIndex, setActiveWordIndex] = useState(-1);
  const [_activeSpeaker, setActiveSpeaker] = useState(-1);
  const [activeMonologueIndex, setActiveMonologueIndex] = useState(-1);
  const [error, setError] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  
  // Refs
  const audioRef = useRef(null);
  const transcriptRef = useRef(null);
  const activeWordRef = useRef(null);

  // Audio and controls
  useEffect(() => {

    const audio = audioRef.current;
    if(!audio || !audioUrl) return;

    // Set up event listeners
    const handleLoadStart = () => {
        setIsLoading(true);
        setError(null);
    };

    const handleLoadedMetadata = () => {
        setDuration(audio.duration);
        setIsLoading(false);
      };

      const handleTimeUpdate = () => {
        setCurrentTime(audio.currentTime);
        updateActiveWord(audio.currentTime);
      };

      const handleEnded = () => {
        setIsPlaying(false);
        setActiveWordIndex(-1);
        setActiveSpeaker(-1);
        setActiveMonologueIndex(-1);
      };

      const handleError = (e) => {
        setError('Unable to load audio file. Please check the URL and try again.');
        setIsLoading(false);
        setIsPlaying(false);
        console.error('Audio loading error:', e);
      };
  
      const handleCanPlay = () => {
        setIsLoading(false);
      };

      audio.addEventListener('loadstart', handleLoadStart);
      audio.addEventListener('loadedmetadata', handleLoadedMetadata);
      audio.addEventListener('timeupdate', handleTimeUpdate);
      audio.addEventListener('ended', handleEnded);
      audio.addEventListener('error', handleError);
      audio.addEventListener('canplay', handleCanPlay);

      return () => {
        audio.removeEventListener('loadstart', handleLoadStart);
        audio.removeEventListener('loadedmetadata', handleLoadedMetadata);
        audio.removeEventListener('timeupdate', handleTimeUpdate);
        audio.removeEventListener('ended', handleEnded);
        audio.removeEventListener('error', handleError);
        audio.removeEventListener('canplay', handleCanPlay);
      };
  }, [audioUrl]);


  /**  Update the active word based on current playback time.  Searches through transcript data to find the word being spoken **/
  const updateActiveWord = (time) => {
    if (!transcriptData) return;

    if (transcriptType === 'monologue' && transcriptData.monologues) {
        let foundWordIndex = -1;
        let foundSpeakerIndex = -1;
        let foundMonologueIndex = -1;
  
        // Search through all monologues and elements to find active word
        transcriptData.monologues.forEach((monologue, monologueIdx) => {
          monologue.elements?.forEach((element, elementIdx) => {
            if (element.type === 'text' && element.ts !== null && element.end_ts !== null) {
              // Check if current time falls within this word's timespan
              if (time >= element.ts && time <= element.end_ts) {
                foundWordIndex = elementIdx;
                foundSpeakerIndex = monologue.speaker;
                foundMonologueIndex = monologueIdx;
              }
            }
          });
        });
  
        // Only update state if the active word changed
        if (foundWordIndex !== activeWordIndex || 
            foundMonologueIndex !== activeMonologueIndex) {
          setActiveWordIndex(foundWordIndex);
          setActiveSpeaker(foundSpeakerIndex);
          setActiveMonologueIndex(foundMonologueIndex);
        }
      } else if (transcriptType === 'full' && transcriptData.lines) {
        // For full transcript format, find the active line
        let foundLineIndex = -1;
  
        transcriptData.lines.forEach((line, lineIdx) => {
          if (time >= line.startTime && time <= line.endTime) {
            foundLineIndex = lineIdx;
          }
        });
  
        if (foundLineIndex !== activeWordIndex) {
          setActiveWordIndex(foundLineIndex);
          setActiveSpeaker(transcriptData.lines[foundLineIndex]?.speaker ?? -1);
        }
      }
  }


  /** Toggle play/pause state **/
  const togglePlayPause = () => {
    const audio = audioRef.current;
    if (!audio || !audioUrl) return;

    if (isPlaying) {
        audio.pause();
    } else {
        audio.play().catch(err => {
            console.error(`Play error: ${err}`);
            setError('Unable to play audio. Please try again.');
        });
    }
    setIsPlaying(!isPlaying);
  };



  /** Skip forward 15 seconds **/
  const skipForward = (seconds = 15) => {
    const audio = audioRef.current;
    if (!audio || !audioUrl) return;

    let newTime = Math.min(audio.currentTime + seconds, duration);
    audio.currentTime = newTime;
    setCurrentTime(newTime);
  }



  /** Skip backwards 15 seconds **/
  const skipBackward = (seconds = 15) => {
    const audio = audioRef.current;
    if (!audio || !audioUrl) return;

    let newTime = Math.max(audio.currentTime - seconds, 0);
    audio.currentTime = newTime;
    setCurrentTime(newTime);
  }



  /** Handle progress bar click to seek  **/
  const handleProgressClick = (e) => {
    const audio = audioRef.current;
    if (!audio) return;

    let progressBar = e.currentTarget;
    let rect = progressBar.getRoundingClientRect();
    let clickPosition = (e.clientX - rect.left) / rect.width;
    let newTime = clickPosition * duration;

    audio.currentTime = newTime;
    setCurrentTime(newTime);
  }



  /**   
    Handle clicking on a word to seek to the that timestamp
    @param (number) timestamp - Start time of the word  
  **/
  const handleWordClick = (timestamp) => {
    const audio = audioRef.current;
    if (!audio || timestamp === null) return;

    audio.currentTime = timestamp;
    setCurrentTime(timestamp);

    // If not playing, start playing
    if (!isPlaying) {
        audio.play().catch(err => console.error(`Play error: ${err}`));
        setIsPlaying(true);
    }
  }



  /** 
    Form  time in MM:SS format
    @param {number} time - Time in seconds
    @returns {string} Formatted time string
  **/
  const formatTime = (time) => {
    if (isNaN(time)) return '00:00';
    let minutes = Math.floor(time / 60);
    let seconds = Math.floor(time % 60);
    return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  }



  /**  
    Render monologue format transcript {word-by-word}
    Each word is displayed inline and highlighted when spoken
  **/
  const renderMonologue = () => {
    if (!transcriptData?.monologues) return null;

    return transcriptData.monologues.map((monologue, monologueIdx) => (
        <div key={monologueIdx} className="speaker-section mb-2">
            <h5 className="speaker-label">Speaker {monologue.speaker}</h5>
            <div className="transcript-text">
                {monologue.elements?.map((element, elementIdx) => {
                    let isActive = activeMonologueIndex === monologueIdx && activeWordIndex === elementIdx && element.type === 'text';

                    if (element.type === 'text') {
                        return (
                            <spam key={`${monologueIdx}-${elementIdx}`}
                                  ref={isActive ? activeWordRef : null}
                                  className={`transcript-word ${isActive ? 'active' : ''}`}
                                  data-start={element.ts}
                                  data-end={element.end_ts}
                                  onClick={() => handleWordClick(element.ts)}
                                  role="button"
                                  tabIndex={0}
                                  aria-label={`${element.value}, click to play from this word`}
                                  onKeyPress={(e) => {
                                    if (e.key === 'Endter' || e.key === ' ') {
                                        handleWordClick(element.ts);
                                    }
                                  }}                                  
                            >
                                {element.value}
                            </spam>
                        );
                    } else if (element.type === 'punct') {
                        return (
                            <span key={`${monologueIdx}-${elementIdx}`} className="transcript-punct">{element.value}</span>
                        );
                    }
                    return null;
                })}
            </div>
        </div>
    ));
  }



  /**  
    Render full transcript format (sentence-by-sentence)
    Highlights the current sentence being spoken
  **/
  const renderFullTranscript = () => {
    if (!transcriptData?.lines) {
        return (
            <div className="alert alert-info">
                <p className="mb-0">
                    Full transcript format not available. Please use monologe format for word-by-word highlighting.
                </p>
            </div>
        );
    }

    return transcriptData.lines.map((line, lineIdx) => {
        let isActive = activeWordIndex === lineIdx;

        return (
            <div key={lineIdx} className={`transcript-line mb-3 ${isActive ? 'active' : ''}`}>
                <div className="line-header">
                    <span className="speaker-badge">Speaker {line.speaker}</span>
                    <span className="time-badge">{formatTime(line.startTime)}</span>
                </div>
                <p  className="line-text"
                    onClick={() => handleWordClick(line.startTime)}
                    role="button"
                    tabIndex={0}
                    onKeyPress={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                            handleWordClick(line.startTime);
                        }
                    }}
                >
                    {line.text}  
                </p>
            </div>            
        );
    })
  }



  /** Auto-scroll to keep active word visible **/
  useEffect(() => {
    if (activeWordRef.current && transcriptRef.current) {
        let wordElement = activeWordRef.current;
        let containerElement = transcriptRef.current;

        // Get Positions
        let wordRect = wordElement.getBoundingClientRect();
        let containerRect = containerElement.getBoundingClientRect();

        // Check if word is outside visible area
        let isAbove = wordRect.top < containerRect.top;
        let isBelow = wordRect.bottom > containerRect.bottom;

        if (isAbove || isBelow) {
            wordElement.scrollIntoView({
                behavior: 'smooth',
                block: 'center',
                inline: 'nearest'
            });
        }
    }
  }, [activeWordIndex, activeMonologueIndex]);



  // JSX UI
  return (
    <div className="audio-player-container">
        <div className="container-fluid">
            <div className="row">

                {/* Audio Player Controls */}
                <div className="col-12">
                    <div className="card shadow-sm mb-4">
                        <div className="card-body">
                            <h4 className="card-title mb-3">
                                <i className="bi bi-file-earmark-music me-2"></i>
                                Audio Player
                            </h4>

                            {/* Error Message */}
                            {error && (
                                <div className="alert alert-danger alert-dismissible fade show" role="alert">
                                    <i className="bi bi-exclamation-triangle me-2"></i>
                                    {error}
                                    <button type="button"
                                            className="btn-close"
                                            onClick={() => setError(null)}
                                            aria-label="Close"
                                    ></button>
                                </div>
                            )}

                            {/* Loading Indicator */}
                            {isLoading && (
                                <div className="alert alert-info d-flex align-items-center" role="status">
                                    <div className="spinner-border spinner-border-sm me-2" role="status">
                                        <span className="visually-hidden">Loading...</span>
                                    </div>
                                    Loading audio...
                                </div>
                            )}


                            
                            {/* Hidden audio element - supports any URL */}
                            <audio  ref={audioRef} 
                                    src={audioUrl} 
                                    preload="metadata"
                                    crossOrigin="anonymous"
                            />


                            {/* Time Display */}
                            <div className="time-display d-flex justify-content-between mb-2">
                                <span className="current-time">{formatTime(currentTime)}</span>
                                <span className="duration">{formatTime(duration)}</span>
                            </div>
                            
                            {/* Progress Bar */}
                            <div    className="progress mb-3" 
                                    style={{ height: '8px', cursor: 'pointer' }}
                                    onClick={handleProgressClick}
                                    role="progressbar"
                                    aria-label="Audio progress"
                                    aria-valuenow={(currentTime / duration) * 100 || 0}
                                    aria-valuemin="0"
                                    aria-valuemax="100"
                            >
                                <div className="progress-bar bg-primary" style={{ width: `${(currentTime / duration) * 100 || 0}%` }} />
                            </div>


                            {/* Control Buttons */}
                            <div className="controls d-flex justify-content-center align-items-center gap-3">
                                <button className="btn btn-outline-primary" onClick={() => skipBackward(15)} disabled={!audioUrl || isLoading} aria-label='Skip backwards 15 second' title="Skip backward 15 seconds.">
                                    <i className="bi bi-skip-backward-fill"> -15s</i>
                                </button>


                                <button className="btn btn-primary btn-lg" onClick={togglePlayPause} disabled={!audioUrl || isLoading} aria-label={isPlaying ? 'Pause' : 'Play'} title={isPlaying ? 'Pause' : 'Play'}>
                                    {isPlaying ? (
                                        <><i className="bi bi-pause-fill"></i> Pause</>
                                    ) : (
                                        <><i className="bi bi-play-fill"></i> Play</>
                                    )}
                                </button>

                                <button className="btn btn-outline-primary" onClick={() => skipForward(15)} disabled={!audioUrl || isLoading} aria-label='Skip forward 15 seconds' title="Skip forward 15 seconds">
                                    +15s <i className="bi bi-skip-forward-fill"></i>
                                </button>

                            </div>


                            {/* Audio URL Info */}
                            {audioUrl && (
                                <div className="audio-info mt-3">
                                    <small className="text-muted">
                                        <i className="bi bi-link-45deg me-1"></i>
                                        Audio Source: {new URL(audioUrl).hostname}
                                    </small>
                                </div>
                            )}
                        </div>
                    </div>
                </div>

                {/* Transcript Display */}
                <div className="col-12">
                    <div className="card shadow-sm">
                        <div className="card-body">
                            <h4 className="card-title mb-3">
                                <i className="bi bi-file-text me-2"></i>
                                Transcript
                                <span className="badge bg-secondary ms-2 fs-6">
                                    {transcriptType === 'monologue' ? 'Word-by-Word' : 'Full Text'}
                                </span>
                            </h4>

                            {transcriptData ? (
                                <div className="transcript-container" 
                                     role="region" 
                                     aria-label="Audio transcript with synchronized highlighting"
                                     aria-live="polite"
                                     ref={transcriptRef}                                     
                                >
                                    {transcriptType === 'monologue' ? renderMonologue() : renderFullTranscript()}
                                </div>
                            ) : (
                                <div className="alert alert-warning">
                                    <i className="bi bi-exclamation-circle me-2"></i>
                                    No transcript data available.
                                </div>
                            )}
                        </div>
                    </div>
                </div>

            </div>
        </div>
    </div>
  )

}

export default AudioPlayer