#!/bin/bash
# analyze_ongonetready_timing.sh
# Analyzes OnGONetReady timing for all GONetParticipants (client and server)

LOGFILE="${1:-C:/Users/shash/AppData/LocalLow/Galore Interactive/GONetSandbox/logs/gonet-2025-10-11.log}"

if [ ! -f "$LOGFILE" ]; then
    echo "ERROR: Log file not found: $LOGFILE"
    exit 1
fi

echo "========================================"
echo "OnGONetReady Timing Analysis"
echo "========================================"
echo "Log file: $LOGFILE"
echo ""

# Create temp files
TEMP_DIR=$(mktemp -d)
START_FILE="$TEMP_DIR/start.txt"
READY_FILE="$TEMP_DIR/ready.txt"
RESULTS_FILE="$TEMP_DIR/results.txt"

# Extract Start() events - simplest approach with grep/sed
grep "Start() called" "$LOGFILE" | sed -n 's/.*\[\(Client:[0-9]*\|Server\)\].*frame:\([0-9]*\).*GONetId: \([0-9]*\).*GameObject: \([^,]*\).*/\1\t\3\t\2\t\4/p' > "$START_FILE"

# Extract OnGONetReady FIRED events
grep "OnGONetReady FIRED" "$LOGFILE" | sed -n 's/.*\[\(Client:[0-9]*\|Server\)\].*frame:\([0-9]*\).*GONetId: \([0-9]*\).*/\1\t\3\t\2/p' > "$READY_FILE"

echo "Extracted $(wc -l < "$START_FILE") Start() events"
echo "Extracted $(wc -l < "$READY_FILE") OnGONetReady FIRED events"
echo ""

# Join Start and Ready data
echo -e "Peer\tGONetId\tGameObject\tStartFrame\tReadyFrame\tFrameDelay" > "$RESULTS_FILE"

while IFS=$'\t' read -r peer gonetid start_frame gameobject; do
    ready_frame=$(grep "^${peer}	${gonetid}	" "$READY_FILE" | head -1 | cut -f3)
    
    if [ -n "$ready_frame" ]; then
        frame_delay=$((ready_frame - start_frame))
        echo -e "$peer\t$gonetid\t$gameobject\t$start_frame\t$ready_frame\t$frame_delay" >> "$RESULTS_FILE"
    else
        echo -e "$peer\t$gonetid\t$gameobject\t$start_frame\tNEVER\tNEVER" >> "$RESULTS_FILE"
    fi
done < "$START_FILE"

# Calculate statistics
echo "========================================"
echo "SUMMARY BY PEER"
echo "========================================"
echo ""

for peer in "Server" "Client:1"; do
    peer_data=$(grep "^${peer}	" "$RESULTS_FILE" | tail -n +2)
    
    if [ -z "$peer_data" ]; then
        echo "--- $peer ---"
        echo "No participants found"
        echo ""
        continue
    fi
    
    echo "--- $peer ---"
    
    total=$(echo "$peer_data" | wc -l)
    fired=$(echo "$peer_data" | grep -v "NEVER" | wc -l)
    never_fired=$(echo "$peer_data" | grep "NEVER" | wc -l)
    
    echo "Total participants: $total"
    echo "✅ OnGONetReady fired: $fired ($(awk "BEGIN {printf \"%.1f\", ($fired/$total)*100}")%)"
    echo "❌ OnGONetReady NEVER fired: $never_fired"
    
    if [ $fired -gt 0 ]; then
        avg_delay=$(echo "$peer_data" | grep -v "NEVER" | awk '{print $6}' | awk '{sum+=$1; count++} END {printf "%.2f", sum/count}')
        min_delay=$(echo "$peer_data" | grep -v "NEVER" | awk '{print $6}' | sort -n | head -1)
        max_delay=$(echo "$peer_data" | grep -v "NEVER" | awk '{print $6}' | sort -n | tail -1)
        
        echo ""
        echo "Average frame delay: $avg_delay frames"
        echo "Min frame delay: $min_delay frame(s)"
        echo "Max frame delay: $max_delay frame(s)"
        
        echo ""
        echo "Frame delay distribution:"
        echo "$peer_data" | grep -v "NEVER" | awk '{print $6}' | sort -n | uniq -c | awk -v total="$fired" '{printf "  %2d frames: %4d participants (%5.1f%%)\n", $2, $1, ($1/total)*100}'
    fi
    
    echo ""
done

echo "========================================"
echo "PARTICIPANTS THAT NEVER FIRED OnGONetReady"
echo "========================================"
echo ""

never_count=$(grep "NEVER" "$RESULTS_FILE" | tail -n +2 | wc -l)

if [ $never_count -gt 0 ]; then
    echo "Found $never_count participants that NEVER fired OnGONetReady:"
    echo ""
    grep "NEVER" "$RESULTS_FILE" | tail -n +2 | awk -F'\t' '{printf "[%s] GONetId: %s, GameObject: %s, StartFrame: %s\n", $1, $2, $3, $4}'
else
    echo "✅ ALL participants successfully fired OnGONetReady!"
fi

echo ""
echo "========================================"
echo "BREAKDOWN BY GAMEOBJECT TYPE"
echo "========================================"
echo ""

grep -v "^Peer" "$RESULTS_FILE" | awk -F'\t' '
{
    name = $3;
    gsub(/\(Clone\)/, "", name);
    gsub(/^ +| +$/, "", name);
    
    if ($6 == "NEVER") {
        never[name]++;
    } else {
        count[name]++;
        sum[name] += $6;
        if (min[name] == "" || $6 < min[name]) min[name] = $6;
        if (max[name] == "" || $6 > max[name]) max[name] = $6;
    }
}
END {
    for (name in count) {
        avg = sum[name] / count[name];
        never_count = (never[name] ? never[name] : 0);
        printf "%s:\n", name;
        printf "  ✅ Fired: %d, ❌ Never: %d, Avg: %.2f frames, Min: %d, Max: %d\n\n", count[name], never_count, avg, min[name], max[name];
    }
    for (name in never) {
        if (!(name in count)) {
            printf "%s:\n", name;
            printf "  ✅ Fired: 0, ❌ Never: %d\n\n", never[name];
        }
    }
}' | sort

echo ""
echo "========================================"
echo "DETAILED RESULTS SAMPLE"
echo "========================================"
echo ""
echo "First 20 entries:"
head -21 "$RESULTS_FILE" | column -t -s $'\t'

echo ""
echo "Temp files: $TEMP_DIR"
echo "Full results: cat $RESULTS_FILE | column -t -s $'\t' | less"
echo ""
