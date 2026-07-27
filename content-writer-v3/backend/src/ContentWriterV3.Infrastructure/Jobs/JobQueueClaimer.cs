using Npgsql;
using Dapper;
using ContentWriterV3.Domain.Entities;

namespace ContentWriterV3.Infrastructure.Jobs;

public interface IJobQueueClaimer
{
    Task<List<Job>> ClaimJobsAsync(string workerId, int batchSize, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
}

public class JobQueueClaimer : IJobQueueClaimer
{
    private readonly string _connectionString;

    public JobQueueClaimer(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<Job>> ClaimJobsAsync(string workerId, int batchSize, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        const string claimQuery = @"
            WITH claimed AS (
                SELECT id FROM content_writer_v3.jobs
                WHERE status = 'Running' AND (lease_expires_at IS NULL OR lease_expires_at < now())
                ORDER BY created_at
                LIMIT @batchSize
                FOR UPDATE SKIP LOCKED
            )
            UPDATE content_writer_v3.jobs j
            SET
                status = 'Running',
                lease_owner = @workerId,
                lease_expires_at = now() + @leaseDuration,
                attempt_count = attempt_count + 1,
                started_at = COALESCE(started_at, now()),
                updated_at = now()
            FROM claimed
            WHERE j.id = claimed.id
            RETURNING
                j.id, j.campaign_id, j.job_type, j.status, j.payload_json,
                j.idempotency_key, j.attempt_count, j.lease_owner, j.lease_expires_at,
                j.error_code, j.error_message, j.created_at, j.started_at,
                j.completed_at, j.requeued_by_user_id, j.requeued_at,
                j.input_version, j.output_version, j.version, j.updated_at;
        ";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        try
        {
            var jobs = (await connection.QueryAsync<dynamic>(
                claimQuery,
                new
                {
                    workerId,
                    batchSize,
                    leaseDuration = leaseDuration.TotalSeconds
                })).ToList();

            return jobs.Select(MapToJob).ToList();
        }
        finally
        {
            connection.Close();
        }
    }

    private static Job MapToJob(dynamic row)
    {
        return new Job
        {
            Id = row.id,
            CampaignId = row.campaign_id,
            JobType = row.job_type,
            Status = Enum.Parse<JobStatus>(row.status),
            PayloadJson = row.payload_json,
            IdempotencyKey = row.idempotency_key,
            AttemptCount = row.attempt_count,
            LeaseOwner = row.lease_owner,
            LeaseExpiresAt = row.lease_expires_at,
            ErrorCode = row.error_code,
            ErrorMessage = row.error_message,
            CreatedAt = row.created_at,
            StartedAt = row.started_at,
            CompletedAt = row.completed_at,
            RequeuedByUserId = row.requeued_by_user_id,
            RequeuedAt = row.requeued_at,
            InputVersion = row.input_version,
            OutputVersion = row.output_version,
            Version = row.version,
            UpdatedAt = row.updated_at
        };
    }
}
