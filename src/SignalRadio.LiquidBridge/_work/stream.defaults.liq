set("tag.encodings",["UTF-8","ISO-8859-1"])

# Configure Logging
set("log.file",false)
set("log.level",1)
set("log.stdout",true)
set("log.syslog",false)
set("log.syslog.facility","DAEMON")
set("log.syslog.program","liquidsoap-#{STREAMID}")

# create a socket to send commands to this instance of liquidsoap
set("server.socket",true)
set("server.socket.path","#{STREAMID}.sock")
set("server.socket.permissions",511)
# This creates a 1 second silence period generated programmatically (no disk reads)
silence = blank(duration=1.)

# This pulls the alpha tag out of the wav file
def append_title(m) =
	[("title",">> Scanning <<")]
end

silence = map_metadata(append_title, silence)

recorder_queue = request.queue()
recorder_queue = server.insert_metadata(id="S4",recorder_queue)

# If there is anything in the queue, play it.  If not, play the silence defined above repeatedly:
stream = fallback(track_sensitive=false, [recorder_queue, silence])

title = '$(if $(title),"$(title)","...Scanning...")'
stream = rewrite_metadata([("title", title)], stream)

output.icecast( %mp3(stereo=false, bitrate=16, samplerate=22050),
  host=HOST, port=PORT, password=PASSWORD, genre=GENRA,
  description=DESCRIPTION, mount=MOUNT,  name=NAME, user="source", stream)